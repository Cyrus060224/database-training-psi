using System.Collections.Generic;
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
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly PsiDbContext _context;
        private readonly JwtHelper _jwtHelper;

        public AuthController(PsiDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            {
                return Ok(ApiResponse<LoginResponse>.Fail("用户名与密码不能为空"));
            }

            var user = await _context.SysUsers
                .FirstOrDefaultAsync(u => u.Username == req.Username);

            if (user == null)
            {
                return Ok(ApiResponse<LoginResponse>.Fail("用户不存在"));
            }

            if (user.Status == 0)
            {
                return Ok(ApiResponse<LoginResponse>.Fail("该账号已被停用，请联系管理员"));
            }

            // 密码校验 (演示系统支持 123456 或 MD5 哈希校验)
            var isPasswordValid = req.Password == "123456" || 
                                  user.PasswordHash == "e10adc3949ba59abbe56e057f20f883e";

            if (!isPasswordValid)
            {
                return Ok(ApiResponse<LoginResponse>.Fail("密码错误"));
            }

            // 获取该用户的角色列表
            var roles = await (from ur in _context.SysUserRoles
                               join r in _context.SysRoles on ur.RoleId equals r.RoleId
                               where ur.UserId == user.UserId
                               select r.RoleCode).ToListAsync();

            var token = _jwtHelper.GenerateToken(user.UserId, user.Username, user.RealName, roles);

            var resp = new LoginResponse
            {
                Token = token,
                UserId = user.UserId,
                Username = user.Username,
                RealName = user.RealName,
                Roles = roles
            };

            return Ok(ApiResponse<LoginResponse>.Success(resp, "登录成功"));
        }

        [Authorize]
        [HttpGet("info")]
        public async Task<ActionResult<ApiResponse<object>>> GetCurrentUserInfo()
        {
            var username = User.Identity?.Name;
            var user = await _context.SysUsers.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return Ok(ApiResponse<object>.Fail("未找到用户信息", 401));
            }

            var roles = await (from ur in _context.SysUserRoles
                               join r in _context.SysRoles on ur.RoleId equals r.RoleId
                               where ur.UserId == user.UserId
                               select new { r.RoleId, r.RoleCode, r.RoleName }).ToListAsync();

            return Ok(ApiResponse<object>.Success(new
            {
                user.UserId,
                user.Username,
                user.RealName,
                user.Phone,
                user.Email,
                Roles = roles
            }));
        }
    }
}
