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
                    s.Address
                }).ToListAsync();

            return Ok(ApiResponse<object>.Success(list));
        }

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
                    c.CreditLimit
                }).ToListAsync();

            return Ok(ApiResponse<object>.Success(list));
        }
    }
}
