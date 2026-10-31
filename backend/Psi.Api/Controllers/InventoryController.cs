using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Psi.Api.Common;
using Psi.Api.Data;

namespace Psi.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly PsiDbContext _context;

        public InventoryController(PsiDbContext context)
        {
            _context = context;
        }

        [HttpGet("stock")]
        public async Task<ActionResult<ApiResponse<PageResult<object>>>> GetStockOverview(
            [FromQuery] string? keyword,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var q = from i in _context.WmsInventories
                    join g in _context.BaseGoods on i.GoodsId equals g.GoodsId
                    join c in _context.BaseCategories on g.CategoryId equals c.CategoryId into gc
                    from cat in gc.DefaultIfEmpty()
                    where g.Status == 1
                    select new
                    {
                        i.InventoryId,
                        g.GoodsId,
                        g.GoodsCode,
                        g.GoodsName,
                        CategoryName = cat != null ? cat.CategoryName : "未分类",
                        g.Specification,
                        g.Unit,
                        i.CurrentStock,
                        g.MinStockAlert,
                        g.MaxStockAlert,
                        IsAlert = i.CurrentStock <= g.MinStockAlert,
                        i.LastInboundTime,
                        i.LastOutboundTime,
                        i.UpdatedTime
                    };

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(x => x.GoodsName.Contains(keyword) || x.GoodsCode.Contains(keyword));
            }

            var total = await q.CountAsync();
            var items = await q.OrderBy(x => x.CurrentStock)
                               .Skip((pageIndex - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

            return Ok(ApiResponse<PageResult<object>>.Success(new PageResult<object>
            {
                Items = items.Cast<object>().ToList(),
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            }));
        }

        [HttpGet("logs")]
        public async Task<ActionResult<ApiResponse<PageResult<object>>>> GetStockLogs(
            [FromQuery] int? goodsId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 15)
        {
            var q = from l in _context.WmsStockLogs
                    join g in _context.BaseGoods on l.GoodsId equals g.GoodsId
                    join u in _context.SysUsers on l.OperatorId equals u.UserId
                    select new
                    {
                        l.LogId,
                        g.GoodsCode,
                        g.GoodsName,
                        l.ChangeType,
                        ChangeTypeName = l.ChangeType == "PUR_IN" ? "采购验收入库" : (l.ChangeType == "SALE_OUT" ? "销售审核出库" : "期初/盘点调整"),
                        l.RelatedOrderNo,
                        l.BeforeStock,
                        l.ChangeQty,
                        l.AfterStock,
                        OperatorName = u.RealName,
                        l.CreateTime
                    };

            if (goodsId.HasValue && goodsId.Value > 0)
            {
                q = q.Where(x => x.GoodsCode == goodsId.ToString());
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(x => x.CreateTime)
                               .Skip((pageIndex - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

            return Ok(ApiResponse<PageResult<object>>.Success(new PageResult<object>
            {
                Items = items.Cast<object>().ToList(),
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            }));
        }
    }
}
