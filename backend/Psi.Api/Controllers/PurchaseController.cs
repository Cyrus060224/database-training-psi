using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Psi.Api.Common;
using Psi.Api.Data;
using Psi.Api.Models;

namespace Psi.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseController : ControllerBase
    {
        private readonly PsiDbContext _context;

        public PurchaseController(PsiDbContext context)
        {
            _context = context;
        }

        [HttpGet("orders")]
        public async Task<ActionResult<ApiResponse<object>>> GetPurchaseOrders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var q = from o in _context.PurOrders
                    join s in _context.BaseSuppliers on o.SupplierId equals s.SupplierId
                    join u in _context.SysUsers on o.PurchaserId equals u.UserId
                    join k in _context.SysUsers on o.WarehouseKeeperId equals k.UserId into ok
                    from keeper in ok.DefaultIfEmpty()
                    orderby o.OrderDate descending
                    select new
                    {
                        o.OrderId,
                        o.OrderNo,
                        s.SupplierName,
                        PurchaserName = u.RealName,
                        KeeperName = keeper != null ? keeper.RealName : "-",
                        o.TotalAmount,
                        o.OrderStatus,
                        StatusName = o.OrderStatus == 1 ? "已入库" : (o.OrderStatus == 0 ? "待入库" : "已取消"),
                        o.OrderDate,
                        o.InboundDate,
                        o.Remark
                    };

            var total = await q.CountAsync();
            var items = await q.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(ApiResponse<PageResult<object>>.Success(new PageResult<object>
            {
                Items = items.Cast<object>().ToList(),
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            }));
        }

        [HttpGet("orders/{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetPurchaseOrderDetails(int id)
        {
            var order = await (from o in _context.PurOrders
                               join s in _context.BaseSuppliers on o.SupplierId equals s.SupplierId
                               join u in _context.SysUsers on o.PurchaserId equals u.UserId
                               where o.OrderId == id
                               select new
                               {
                                   o.OrderId,
                                   o.OrderNo,
                                   o.SupplierId,
                                   s.SupplierName,
                                   PurchaserName = u.RealName,
                                   o.TotalAmount,
                                   o.OrderStatus,
                                   o.OrderDate,
                                   o.Remark
                               }).FirstOrDefaultAsync();

            if (order == null) return Ok(ApiResponse<object>.Fail("未找到该采购单"));

            var details = await (from d in _context.PurOrderDetails
                                 join g in _context.BaseGoods on d.GoodsId equals g.GoodsId
                                 where d.OrderId == id
                                 select new
                                 {
                                     d.DetailId,
                                     d.GoodsId,
                                     g.GoodsCode,
                                     g.GoodsName,
                                     g.Specification,
                                     g.Unit,
                                     d.PurchasePrice,
                                     d.Quantity,
                                     d.Subtotal
                                 }).ToListAsync();

            return Ok(ApiResponse<object>.Success(new { Order = order, Details = details }));
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<string>>> CreatePurchaseOrder([FromBody] CreatePurOrderDto dto)
        {
            if (dto.SupplierId <= 0)
            {
                return Ok(ApiResponse<string>.Fail("供应商信息无效"));
            }

            var supplier = await _context.BaseSuppliers.FindAsync(dto.SupplierId);
            if (supplier == null || supplier.Status != 1)
            {
                return Ok(ApiResponse<string>.Fail("所选供应商不存在或已停用"));
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                return Ok(ApiResponse<string>.Fail("采购商品清单不能为空"));
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(userIdStr, out var userId);
            if (userId == 0) userId = 1;

            var orderNo = "CG" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            decimal totalAmount = 0;

            var purOrder = new PurOrder
            {
                OrderNo = orderNo,
                SupplierId = dto.SupplierId,
                PurchaserId = userId,
                OrderStatus = 0, // 待入库
                Remark = dto.Remark,
                OrderDate = DateTime.Now
            };

            foreach (var item in dto.Items)
            {
                if (item.GoodsId <= 0)
                {
                    return Ok(ApiResponse<string>.Fail("商品ID无效"));
                }
                if (item.Quantity <= 0)
                {
                    return Ok(ApiResponse<string>.Fail("采购数量必须大于 0"));
                }
                if (item.Price < 0)
                {
                    return Ok(ApiResponse<string>.Fail("采购进价不能小于 0"));
                }

                var goods = await _context.BaseGoods.FindAsync(item.GoodsId);
                if (goods == null || goods.Status != 1)
                {
                    return Ok(ApiResponse<string>.Fail($"商品 (ID: {item.GoodsId}) 不存在或已停用"));
                }

                var subtotal = item.Price * item.Quantity;
                totalAmount += subtotal;
                purOrder.Details.Add(new PurOrderDetail
                {
                    GoodsId = item.GoodsId,
                    PurchasePrice = item.Price,
                    Quantity = item.Quantity,
                    Subtotal = subtotal
                });
            }

            purOrder.TotalAmount = totalAmount;

            _context.PurOrders.Add(purOrder);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Success(orderNo, "采购单创建成功，等待验收入库"));
        }

        // 核心考核点：验收入库事务处理与高并发防重入
        [HttpPost("inbound/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> ExecuteInbound(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(userIdStr, out var keeperId);
            if (keeperId == 0) keeperId = 1;

            // 开启显式数据库事务保证 ACID
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.PurOrders
                    .Include(o => o.Details)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return Ok(ApiResponse<string>.Fail("采购单不存在"));
                }

                if (order.OrderStatus != 0)
                {
                    return Ok(ApiResponse<string>.Fail("该单据已入库或已取消，不可重复操作"));
                }

                // 原子状态抢占流转，防止并发重复提交
                var affected = await _context.PurOrders
                    .Where(o => o.OrderId == id && o.OrderStatus == 0)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(o => o.OrderStatus, 1)
                        .SetProperty(o => o.WarehouseKeeperId, keeperId)
                        .SetProperty(o => o.InboundDate, DateTime.Now));

                if (affected == 0)
                {
                    await transaction.RollbackAsync();
                    return Ok(ApiResponse<string>.Fail("该采购单已被其他操作员入库或取消，不可重复操作！"));
                }

                foreach (var detail in order.Details)
                {
                    var inventory = await _context.WmsInventories
                        .FirstOrDefaultAsync(i => i.GoodsId == detail.GoodsId);

                    int beforeStock = 0;
                    if (inventory == null)
                    {
                        inventory = new WmsInventory
                        {
                            GoodsId = detail.GoodsId,
                            CurrentStock = detail.Quantity,
                            LastInboundTime = DateTime.Now,
                            UpdatedTime = DateTime.Now
                        };
                        _context.WmsInventories.Add(inventory);
                        await _context.SaveChangesAsync();
                        beforeStock = 0;
                    }
                    else
                    {
                        beforeStock = inventory.CurrentStock;
                        inventory.CurrentStock += detail.Quantity;
                        inventory.LastInboundTime = DateTime.Now;
                        inventory.UpdatedTime = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }

                    // 写入库存变动流水日志
                    var log = new WmsStockLog
                    {
                        GoodsId = detail.GoodsId,
                        ChangeType = "PUR_IN",
                        RelatedOrderNo = order.OrderNo,
                        BeforeStock = beforeStock,
                        ChangeQty = detail.Quantity,
                        AfterStock = beforeStock + detail.Quantity,
                        OperatorId = keeperId,
                        CreateTime = DateTime.Now
                    };
                    _context.WmsStockLogs.Add(log);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(ApiResponse<string>.Success("验收入库成功，库存已同步更新"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Ok(ApiResponse<string>.Fail("入库操作发生异常，事务已回滚: " + ex.Message));
            }
        }
    }
}
