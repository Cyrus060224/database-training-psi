using Microsoft.EntityFrameworkCore;
using Psi.Api.Models;

namespace Psi.Api.Data
{
    public class PsiDbContext : DbContext
    {
        public PsiDbContext(DbContextOptions<PsiDbContext> options) : base(options)
        {
        }

        public DbSet<SysRole> SysRoles { get; set; }
        public DbSet<SysUser> SysUsers { get; set; }
        public DbSet<SysUserRole> SysUserRoles { get; set; }

        public DbSet<BaseCategory> BaseCategories { get; set; }
        public DbSet<BaseSupplier> BaseSuppliers { get; set; }
        public DbSet<BaseCustomer> BaseCustomers { get; set; }
        public DbSet<BaseGoods> BaseGoods { get; set; }

        public DbSet<WmsInventory> WmsInventories { get; set; }
        public DbSet<PurOrder> PurOrders { get; set; }
        public DbSet<PurOrderDetail> PurOrderDetails { get; set; }
        public DbSet<SaleOrder> SaleOrders { get; set; }
        public DbSet<SaleOrderDetail> SaleOrderDetails { get; set; }
        public DbSet<WmsStockLog> WmsStockLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置商品与库存 1:1 关系
            modelBuilder.Entity<BaseGoods>()
                .HasOne(g => g.Inventory)
                .WithOne(i => i.Goods)
                .HasForeignKey<WmsInventory>(i => i.GoodsId)
                .OnDelete(DeleteBehavior.Cascade);

            // 采购单与采购明细 1:N
            modelBuilder.Entity<PurOrder>()
                .HasMany(o => o.Details)
                .WithOne(d => d.Order)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // 销售单与销售明细 1:N
            modelBuilder.Entity<SaleOrder>()
                .HasMany(o => o.Details)
                .WithOne(d => d.Order)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
