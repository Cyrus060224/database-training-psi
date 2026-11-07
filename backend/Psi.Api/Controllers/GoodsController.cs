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
    public class GoodsController : ControllerBase
    {
        private readonly PsiDbContext _context;

        public GoodsController(PsiDbContext context)
        {
            _context = context;
        }

        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<PageResult<object>>>> GetGoodsList([FromQuery] GoodsQueryParams query)
        {
            var q = from g in _context.BaseGoods
                    join c in _context.BaseCategories on g.CategoryId equals c.CategoryId into gc
                    from cat in gc.DefaultIfEmpty()
                    join i in _context.WmsInventories on g.GoodsId equals i.GoodsId into gi
                    from inv in gi.DefaultIfEmpty()
                    where g.Status == 1
                    select new
                    {
                        g.GoodsId,
                        g.GoodsCode,
                        g.GoodsName,
                        g.CategoryId,
                        CategoryName = cat != null ? cat.CategoryName : "未分类",
                        g.Barcode,
                        g.Specification,
                        g.Unit,
                        g.CostPrice,
                        g.RetailPrice,
                        g.MinStockAlert,
                        g.MaxStockAlert,
                        CurrentStock = inv != null ? inv.CurrentStock : 0,
                        IsAlert = (inv != null ? inv.CurrentStock : 0) <= g.MinStockAlert,
                        g.CreateTime
                    };

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                q = q.Where(x => x.GoodsName.Contains(query.Keyword) || x.GoodsCode.Contains(query.Keyword));
            }

            if (query.CategoryId.HasValue && query.CategoryId.Value > 0)
            {
                q = q.Where(x => x.CategoryId == query.CategoryId.Value);
            }

            if (query.AlertOnly == true)
            {
                q = q.Where(x => x.IsAlert);
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(x => x.GoodsId)
                               .Skip((query.PageIndex - 1) * query.PageSize)
                               .Take(query.PageSize)
                               .ToListAsync();

            var result = new PageResult<object>
            {
                Items = items.Cast<object>().ToList(),
                Total = total,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };

            return Ok(ApiResponse<PageResult<object>>.Success(result));
        }

        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<object>>> GetCategories()
        {
            var list = await _context.BaseCategories
                .OrderBy(c => c.SortOrder)
                .Select(c => new { c.CategoryId, c.CategoryCode, c.CategoryName })
                .ToListAsync();

            return Ok(ApiResponse<object>.Success(list));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetGoodsById(int id)
        {
            var goods = await _context.BaseGoods.FindAsync(id);
            if (goods == null)
            {
                return Ok(ApiResponse<object>.Fail("商品不存在"));
            }

            return Ok(ApiResponse<object>.Success(goods));
        }

        [HttpPost("save")]
        public async Task<ActionResult<ApiResponse<string>>> SaveGoods([FromBody] GoodsSaveDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.GoodsName))
            {
                return Ok(ApiResponse<string>.Fail("商品名称不能为空"));
            }

            if (dto.CostPrice < 0 || dto.RetailPrice < 0)
            {
                return Ok(ApiResponse<string>.Fail("进价与售价必须大于或等于 0"));
            }

            if (dto.MinStockAlert < 0 || dto.MaxStockAlert < dto.MinStockAlert)
            {
                return Ok(ApiResponse<string>.Fail("库存预警值无效：最高库存必须大于或等于最低库存且大于等于0"));
            }

            var categoryExists = await _context.BaseCategories.AnyAsync(c => c.CategoryId == dto.CategoryId);
            if (!categoryExists)
            {
                return Ok(ApiResponse<string>.Fail("所选商品类别不存在"));
            }

            if (dto.GoodsId == 0)
            {
                // 新增商品
                if (string.IsNullOrWhiteSpace(dto.GoodsCode))
                {
                    dto.GoodsCode = "G" + DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                // 检查商品编号唯一性
                if (await _context.BaseGoods.AnyAsync(g => g.GoodsCode == dto.GoodsCode))
                {
                    return Ok(ApiResponse<string>.Fail("商品编号已存在"));
                }

                var entity = new BaseGoods
                {
                    GoodsCode = dto.GoodsCode,
                    GoodsName = dto.GoodsName,
                    CategoryId = dto.CategoryId,
                    Barcode = dto.Barcode,
                    Specification = dto.Specification,
                    Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "件" : dto.Unit,
                    CostPrice = dto.CostPrice,
                    RetailPrice = dto.RetailPrice,
                    MinStockAlert = dto.MinStockAlert,
                    MaxStockAlert = dto.MaxStockAlert,
                    Status = 1,
                    CreateTime = DateTime.Now
                };

                _context.BaseGoods.Add(entity);
                await _context.SaveChangesAsync();

                // 自动同步初始化库存记录为 0
                var inv = new WmsInventory
                {
                    GoodsId = entity.GoodsId,
                    CurrentStock = 0,
                    UpdatedTime = DateTime.Now
                };
                _context.WmsInventories.Add(inv);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<string>.Success("新增商品成功"));
            }
            else
            {
                // 编辑商品
                var entity = await _context.BaseGoods.FindAsync(dto.GoodsId);
                if (entity == null)
                {
                    return Ok(ApiResponse<string>.Fail("未找到该商品"));
                }

                entity.GoodsName = dto.GoodsName;
                entity.CategoryId = dto.CategoryId;
                entity.Barcode = dto.Barcode;
                entity.Specification = dto.Specification;
                entity.Unit = dto.Unit;
                entity.CostPrice = dto.CostPrice;
                entity.RetailPrice = dto.RetailPrice;
                entity.MinStockAlert = dto.MinStockAlert;
                entity.MaxStockAlert = dto.MaxStockAlert;

                await _context.SaveChangesAsync();
                return Ok(ApiResponse<string>.Success("修改商品信息成功"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteGoods(int id)
        {
            var entity = await _context.BaseGoods.FindAsync(id);
            if (entity == null)
            {
                return Ok(ApiResponse<string>.Fail("商品不存在"));
            }

            // 软删除
            entity.Status = 0;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<string>.Success("删除商品成功"));
        }
    }
}
