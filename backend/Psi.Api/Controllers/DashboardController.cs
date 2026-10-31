using System;
using System.Linq;
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
    public class DashboardController : ControllerBase
    {
        private readonly PsiDbContext _context;

        public DashboardController(PsiDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
        {
            // 1. 销售总额与采购总额
            var totalSales = await _context.SaleOrders
                .Where(o => o.OrderStatus == 1)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalPurchases = await _context.PurOrders
                .Where(o => o.OrderStatus == 1)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // 2. 商品总数与预警数
            var totalGoods = await _context.BaseGoods.CountAsync(g => g.Status == 1);

            var alertCount = await (from g in _context.BaseGoods
                                    join i in _context.WmsInventories on g.GoodsId equals i.GoodsId into gi
                                    from inv in gi.DefaultIfEmpty()
                                    where g.Status == 1 && (inv == null || inv.CurrentStock <= g.MinStockAlert)
                                    select g.GoodsId).CountAsync();

            // 3. 畅销商品 TOP 5
            var topSelling = await (from d in _context.SaleOrderDetails
                                    join o in _context.SaleOrders on d.OrderId equals o.OrderId
                                    join g in _context.BaseGoods on d.GoodsId equals g.GoodsId
                                    where o.OrderStatus == 1
                                    group d by g.GoodsName into grp
                                    orderby grp.Sum(x => x.Quantity) descending
                                    select new TopSellingItemDto
                                    {
                                        GoodsName = grp.Key,
                                        SaleQty = grp.Sum(x => x.Quantity),
                                        SaleAmount = grp.Sum(x => x.Subtotal)
                                    }).Take(5).ToListAsync();

            // 4. 最近单据动态
            var recentSales = await (from o in _context.SaleOrders
                                     join c in _context.BaseCustomers on o.CustomerId equals c.CustomerId
                                     orderby o.OrderDate descending
                                     select new RecentOrderItemDto
                                     {
                                         OrderNo = o.OrderNo,
                                         Type = "销售",
                                         PartnerName = c.CustomerName,
                                         Amount = o.TotalAmount,
                                         StatusName = o.OrderStatus == 1 ? "已出库" : (o.OrderStatus == 0 ? "待出库" : "已作废"),
                                         OrderDate = o.OrderDate.ToString("yyyy-MM-dd HH:mm")
                                     }).Take(5).ToListAsync();

            var recentPurchases = await (from o in _context.PurOrders
                                         join s in _context.BaseSuppliers on o.SupplierId equals s.SupplierId
                                         orderby o.OrderDate descending
                                         select new RecentOrderItemDto
                                         {
                                             OrderNo = o.OrderNo,
                                             Type = "采购",
                                             PartnerName = s.SupplierName,
                                             Amount = o.TotalAmount,
                                             StatusName = o.OrderStatus == 1 ? "已入库" : (o.OrderStatus == 0 ? "待入库" : "已取消"),
                                             OrderDate = o.OrderDate.ToString("yyyy-MM-dd HH:mm")
                                         }).Take(5).ToListAsync();

            var recentOrders = recentSales.Concat(recentPurchases)
                .OrderByDescending(x => x.OrderDate)
                .Take(6)
                .ToList();

            var dto = new DashboardStatsDto
            {
                TotalSaleAmount = totalSales,
                TotalPurchaseAmount = totalPurchases,
                TotalGoodsCount = totalGoods,
                StockAlertCount = alertCount,
                TopSellingGoods = topSelling,
                RecentOrders = recentOrders
            };

            return Ok(ApiResponse<DashboardStatsDto>.Success(dto));
        }

        [HttpGet("stock-alerts")]
        public async Task<ActionResult<ApiResponse<object>>> GetStockAlerts()
        {
            var alerts = await (from g in _context.BaseGoods
                                join c in _context.BaseCategories on g.CategoryId equals c.CategoryId
                                join i in _context.WmsInventories on g.GoodsId equals i.GoodsId into gi
                                from inv in gi.DefaultIfEmpty()
                                let currentStock = inv != null ? inv.CurrentStock : 0
                                where g.Status == 1 && currentStock <= g.MinStockAlert
                                select new
                                {
                                    g.GoodsId,
                                    g.GoodsCode,
                                    g.GoodsName,
                                    c.CategoryName,
                                    g.Specification,
                                    g.Unit,
                                    CurrentStock = currentStock,
                                    g.MinStockAlert,
                                    ShortageQty = g.MinStockAlert - currentStock
                                }).ToListAsync();

            return Ok(ApiResponse<object>.Success(alerts));
        }
    }
}
