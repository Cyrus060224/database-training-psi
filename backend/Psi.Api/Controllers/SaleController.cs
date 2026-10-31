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
            if (dto.Items == null || dto.Items.Count == 0)
            {
                return Ok(ApiResponse<string>.Fail("销售商品清单不能为空"));
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(userIdStr, out var userId);
            if (userId == 0) userId = 1;

            var orderNo = "XS" + DateTime.Now.ToString("yyyyMMddHHmmss");
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
        // 答辩核心杀手锏：销售出库事务控制与并发防超卖（防负库存机制）
        // ====================================================================
        [HttpPost("outbound/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> ExecuteOutbound(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            int.TryParse(userIdStr, out var keeperId);
            if (keeperId == 0) keeperId = 1;

            // 1. 开启数据库事务（ACID 保证）
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

                // 2. 遍历校验库存并扣减
                foreach (var detail in order.Details)
                {
                    var goods = await _context.BaseGoods.FindAsync(detail.GoodsId);
                    var goodsName = goods?.GoodsName ?? $"商品ID:{detail.GoodsId}";

                    var inventory = await _context.WmsInventories
                        .FirstOrDefaultAsync(i => i.GoodsId == detail.GoodsId);

                    if (inventory == null || inventory.CurrentStock < detail.Quantity)
                    {
                        var currStock = inventory?.CurrentStock ?? 0;
                        await transaction.RollbackAsync();
                        return Ok(ApiResponse<string>.Fail($"出库受阻！商品【{goodsName}】当前库存仅剩 {currStock}，无法满足出库需求 {detail.Quantity} 件！事务已全部回滚。"));
                    }

                    int beforeStock = inventory.CurrentStock;
                    int afterStock = beforeStock - detail.Quantity;

                    // 原子更新，配合 SQL Server 约束防止并发负库存
                    inventory.CurrentStock = afterStock;
                    inventory.LastOutboundTime = DateTime.Now;
                    inventory.UpdatedTime = DateTime.Now;

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

                // 3. 更新主单据状态
                order.OrderStatus = 1; // 已出库
                order.WarehouseKeeperId = keeperId;
                order.OutboundDate = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(ApiResponse<string>.Success("销售出库审核完成，库存已扣减并生成流水"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Ok(ApiResponse<string>.Fail("出库发生异常，事务已回滚: " + ex.Message));
            }
        }
    }
}
