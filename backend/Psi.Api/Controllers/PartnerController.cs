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
    public class PartnerController : ControllerBase
    {
        private readonly PsiDbContext _context;

        public PartnerController(PsiDbContext context)
        {
            _context = context;
        }

        // ======================= 供应商管理 =======================

        [HttpGet("suppliers")]
        public async Task<ActionResult<ApiResponse<object>>> GetSuppliers()
        {
            var list = await _context.BaseSuppliers
                .Where(s => s.Status == 1)
                .OrderBy(s => s.SupplierId)
                .Select(s => new
                {
                    s.SupplierId,
                    s.SupplierCode,
                    s.SupplierName,
                    s.ContactPerson,
                    s.Phone,
                    s.Address,
                    s.CreateTime
                }).ToListAsync();

            return Ok(ApiResponse<object>.Success(list));
        }

        [HttpPost("supplier/save")]
        public async Task<ActionResult<ApiResponse<string>>> SaveSupplier([FromBody] SupplierSaveDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SupplierName))
            {
                return Ok(ApiResponse<string>.Fail("供应商名称不能为空"));
            }

            if (dto.SupplierId == 0)
            {
                if (string.IsNullOrWhiteSpace(dto.SupplierCode))
                {
                    dto.SupplierCode = "SUP" + DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                var entity = new BaseSupplier
                {
                    SupplierCode = dto.SupplierCode,
                    SupplierName = dto.SupplierName,
                    ContactPerson = dto.ContactPerson,
                    Phone = dto.Phone,
                    Address = dto.Address,
                    Status = 1,
                    CreateTime = DateTime.Now
                };
                _context.BaseSuppliers.Add(entity);
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<string>.Success("新增供应商成功"));
            }
            else
            {
                var entity = await _context.BaseSuppliers.FindAsync(dto.SupplierId);
                if (entity == null) return Ok(ApiResponse<string>.Fail("供应商不存在"));

                entity.SupplierName = dto.SupplierName;
                entity.ContactPerson = dto.ContactPerson;
                entity.Phone = dto.Phone;
                entity.Address = dto.Address;
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<string>.Success("修改供应商成功"));
            }
        }

        [HttpDelete("supplier/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteSupplier(int id)
        {
            var entity = await _context.BaseSuppliers.FindAsync(id);
            if (entity == null) return Ok(ApiResponse<string>.Fail("供应商不存在"));
            entity.Status = 0; // 软删除
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Success("删除供应商成功"));
        }

        // ======================= 客户管理 =======================

        [HttpGet("customers")]
        public async Task<ActionResult<ApiResponse<object>>> GetCustomers()
        {
            var list = await _context.BaseCustomers
                .Where(c => c.Status == 1)
                .OrderBy(c => c.CustomerId)
                .Select(c => new
                {
                    c.CustomerId,
                    c.CustomerCode,
                    c.CustomerName,
                    c.ContactPerson,
                    c.Phone,
                    c.Address,
                    c.CreditLimit,
                    c.CreateTime
                }).ToListAsync();

            return Ok(ApiResponse<object>.Success(list));
        }

        [HttpPost("customer/save")]
        public async Task<ActionResult<ApiResponse<string>>> SaveCustomer([FromBody] CustomerSaveDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                return Ok(ApiResponse<string>.Fail("客户名称不能为空"));
            }

            if (dto.CreditLimit < 0)
            {
                return Ok(ApiResponse<string>.Fail("客户授信额度不能小于 0"));
            }

            if (dto.CustomerId == 0)
            {
                if (string.IsNullOrWhiteSpace(dto.CustomerCode))
                {
                    dto.CustomerCode = "CUST" + DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                var entity = new BaseCustomer
                {
                    CustomerCode = dto.CustomerCode,
                    CustomerName = dto.CustomerName,
                    ContactPerson = dto.ContactPerson,
                    Phone = dto.Phone,
                    Address = dto.Address,
                    CreditLimit = dto.CreditLimit,
                    Status = 1,
                    CreateTime = DateTime.Now
                };
                _context.BaseCustomers.Add(entity);
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<string>.Success("新增客户成功"));
            }
            else
            {
                var entity = await _context.BaseCustomers.FindAsync(dto.CustomerId);
                if (entity == null) return Ok(ApiResponse<string>.Fail("客户不存在"));

                entity.CustomerName = dto.CustomerName;
                entity.ContactPerson = dto.ContactPerson;
                entity.Phone = dto.Phone;
                entity.Address = dto.Address;
                entity.CreditLimit = dto.CreditLimit;
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<string>.Success("修改客户成功"));
            }
        }

        [HttpDelete("customer/{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteCustomer(int id)
        {
            var entity = await _context.BaseCustomers.FindAsync(id);
            if (entity == null) return Ok(ApiResponse<string>.Fail("客户不存在"));
            entity.Status = 0; // 软删除
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<string>.Success("删除客户成功"));
        }
    }
}
