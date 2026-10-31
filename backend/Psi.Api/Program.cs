using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Psi.Api.Common;
using Psi.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. 配置数据库上下文 (SQL Server)
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PsiDbContext>(options =>
{
    options.UseSqlServer(connStr);
});

// 2. 注册自定义服务与工具
builder.Services.AddScoped<JwtHelper>();

// 3. 配置 JWT 身份认证
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "PsiSecuritySecretKey2026SuperStrongKeyForJwtTokenAuth";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "PsiApi";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "PsiWebClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true
    };
});

// 4. 配置跨域支持 (CORS) - 允许前端 Vue 3 (Vite 默认 5173 端口) 访问
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

// 5. 配置 Swagger 文档，并加入 JWT Token 授权测试输入框
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "商品购销存管理系统 API (PSI System)",
        Version = "v1.0",
        Description = "南京审计大学《数据库开发与应用综合实训》- ASP.NET Core 8 Web API 后端服务"
    });

    // 启用 Swagger 右上角 Authorize 锁头图标
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "请输入 JWT Token，格式为: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 启用 Swagger（开发与生产模式下均可查看，方便答辩展示）
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PSI System API v1.0");
    c.RoutePrefix = "swagger"; // 访问地址: /swagger
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
