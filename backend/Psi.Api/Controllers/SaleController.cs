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
    public class SaleController : ControllerBase
    {
        private readonly PsiDbContext _context;

        public SaleController(PsiDbContext context)
        {
            _context = context;
        }

        [HttpGet("orders")]
        public async Task<ActionResult<ApiResponse<object>>> GetSaleOrders([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var q = from o in _context.SaleOrders
                    join c in _context.BaseCustomers on o.CustomerId equals c.CustomerId
                    join u in _context.SysUsers on o.SalerId equals u.UserId
                    join k in _context.SysUsers on o.WarehouseKeeperId equals k.UserId into ok
                    from keeper in ok.DefaultIfEmpty()
                    orderby o.OrderDate descending
                    select new
                    {
                        o.OrderId,
                        o.OrderNo,
                        c.CustomerName,
                        SalerName = u.RealName,
                        KeeperName = keeper != null ? keeper.RealName : "-",
                        o.TotalAmount,
                        o.OrderStatus,
                        StatusName = o.OrderStatus == 1 ? "已出库" : (o.OrderStatus == 0 ? "待出库" : "已作废"),
                        o.OrderDate,
                        o.OutboundDate,
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
        public async Task<ActionResult<ApiResponse<object>>> GetSaleOrderDetails(int id)
        {
            var order = await (from o in _context.SaleOrders
                               join c in _context.BaseCustomers on o.CustomerId equals c.CustomerId
                               join u in _context.SysUsers on o.SalerId equals u.UserId
                               where o.OrderId == id
                               select new
                               {
                                   o.OrderId,
                                   o.OrderNo,
                                   o.CustomerId,
                                   c.CustomerName,
                                   SalerName = u.RealName,
                                   o.TotalAmount,
                                   o.OrderStatus,
                                   o.OrderDate,
                                   o.Remark
                               }).FirstOrDefaultAsync();

            if (order == null) return Ok(ApiResponse<object>.Fail("未找到该销售单"));

            var details = await (from d in _context.SaleOrderDetails
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
                                     d.SalePrice,
                                     d.Quantity,
                                     d.Subtotal
                                 }).ToListAsync();

            return Ok(ApiResponse<object>.Success(new { Order = order, Details = details }));
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<string>>> CreateSaleOrder([FromBody] CreateSaleOrderDto dto)
        {
            if (dto.CustomerId <= 0)
            {
                return Ok(ApiResponse<string>.Fail("客户信息无效"));
            }

            var customer = await _context.BaseCustomers.FindAsync(dto.CustomerId);
            if (customer == null || customer.Status != 1)
            {
                return Ok(ApiResponse<string>.Fail("所选客户不存在或已注销停用"));
            }

            if (dto.Items == null || dto.Items.Count == 0)
            {
                return Ok(ApiResponse<string>.Fail("销售商品清单不能为空"));
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(userIdStr, out var userId);
            if (userId == 0) userId = 1;

            var orderNo = "XS" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            decimal totalAmount = 0;

            var saleOrder = new SaleOrder
            {
                OrderNo = orderNo,
                CustomerId = dto.CustomerId,
                SalerId = userId,
                OrderStatus = 0, // 待出库
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
                    return Ok(ApiResponse<string>.Fail("商品销售数量必须大于 0"));
                }
                if (item.Price < 0)
                {
                    return Ok(ApiResponse<string>.Fail("商品销售单价不能小于 0"));
                }

                var goods = await _context.BaseGoods.FindAsync(item.GoodsId);
                if (goods == null || goods.Status != 1)
                {
                    return Ok(ApiResponse<string>.Fail($"商品 (ID: {item.GoodsId}) 不存在或已下架停售"));
                }

                var subtotal = item.Price * item.Quantity;
                totalAmount += subtotal;
                saleOrder.Details.Add(new SaleOrderDetail
                {
                    GoodsId = item.GoodsId,
                    SalePrice = item.Price,
                    Quantity = item.Quantity,
                    Subtotal = subtotal
                });
            }

            saleOrder.TotalAmount = totalAmount;

            _context.SaleOrders.Add(saleOrder);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Success(orderNo, "销售单创建成功，等待仓管审核出库"));
        }

        // ====================================================================
        // 核心实训与答辩杀手锏：销售出库高并发原子扣减与防超卖机制（ACID + 行级排他更新）
        // ====================================================================
        [HttpPost("outbound/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> ExecuteOutbound(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(userIdStr, out var keeperId);
            if (keeperId == 0) keeperId = 1;

            // 1. 开启显式数据库事务（ACID 完整隔离）
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.SaleOrders
                    .Include(o => o.Details)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return Ok(ApiResponse<string>.Fail("销售单不存在"));
                }

                if (order.OrderStatus != 0)
                {
                    return Ok(ApiResponse<string>.Fail("该订单已出库或已作废，不可重复操作"));
                }

                // 2. 状态原子流转与防并发重复提交
                var affectedOrder = await _context.SaleOrders
                    .Where(o => o.OrderId == id && o.OrderStatus == 0)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(o => o.OrderStatus, 1)
                        .SetProperty(o => o.WarehouseKeeperId, keeperId)
                        .SetProperty(o => o.OutboundDate, DateTime.Now));

                if (affectedOrder == 0)
                {
                    await transaction.RollbackAsync();
                    return Ok(ApiResponse<string>.Fail("该单据已被其他操作员处理或作废，不可重复出库！"));
                }

                // 3. 遍历商品进行数据库行级原子扣减（超卖拦截关键点）
                foreach (var detail in order.Details)
                {
                    var goods = await _context.BaseGoods.FindAsync(detail.GoodsId);
                    var goodsName = goods?.GoodsName ?? $"商品ID:{detail.GoodsId}";

                    // 利用 SQL 引擎行级排他锁原子检查库存并扣减：WHERE current_stock >= detail.Quantity
                    var affectedInv = await _context.WmsInventories
                        .Where(i => i.GoodsId == detail.GoodsId && i.CurrentStock >= detail.Quantity)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(i => i.CurrentStock, i => i.CurrentStock - detail.Quantity)
                            .SetProperty(i => i.LastOutboundTime, DateTime.Now)
                            .SetProperty(i => i.UpdatedTime, DateTime.Now));

                    if (affectedInv == 0)
                    {
                        var currStock = await _context.WmsInventories
                            .Where(i => i.GoodsId == detail.GoodsId)
                            .Select(i => (int?)i.CurrentStock)
                            .FirstOrDefaultAsync() ?? 0;

                        // 核心：任一商品库存不足即整单原子回滚，不留残余脏数据与幽灵扣减
                        await transaction.RollbackAsync();
                        return Ok(ApiResponse<string>.Fail($"出库受阻！商品【{goodsName}】当前可用库存仅剩 {currStock} 件，无法满足销售需求 {detail.Quantity} 件！整单事务已全量回滚。"));
                    }

                    // 查询当前扣减后库存用于写入精准审计流水
                    var afterStock = await _context.WmsInventories
                        .Where(i => i.GoodsId == detail.GoodsId)
                        .Select(i => i.CurrentStock)
                        .FirstOrDefaultAsync();
                    var beforeStock = afterStock + detail.Quantity;

                    // 写入出库审计流水
                    var log = new WmsStockLog
                    {
                        GoodsId = detail.GoodsId,
                        ChangeType = "SALE_OUT",
                        RelatedOrderNo = order.OrderNo,
                        BeforeStock = beforeStock,
                        ChangeQty = -detail.Quantity,
                        AfterStock = afterStock,
                        OperatorId = keeperId,
                        CreateTime = DateTime.Now
                    };
                    _context.WmsStockLogs.Add(log);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(ApiResponse<string>.Success("销售出库审核完成，库存已原子扣减并生成审计流水"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Ok(ApiResponse<string>.Fail("出库发生异常，事务已回滚: " + ex.Message));
            }
        }
    }
}
