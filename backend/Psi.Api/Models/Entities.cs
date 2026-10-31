using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Psi.Api.Models
{
    // ==========================================
    // 系统与权限模块 (RBAC)
    // ==========================================

    [Table("sys_role")]
    public class SysRole
    {
        [Key]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("role_code")]
        public string RoleCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("role_name")]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(200)]
        [Column("description")]
        public string? Description { get; set; }

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    [Table("sys_user")]
    public class SysUser
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("real_name")]
        public string RealName { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("phone")]
        public string? Phone { get; set; }

        [MaxLength(100)]
        [Column("email")]
        public string? Email { get; set; }

        [Column("status")]
        public byte Status { get; set; } = 1;

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    [Table("sys_user_role")]
    public class SysUserRole
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("role_id")]
        public int RoleId { get; set; }

        [ForeignKey("UserId")]
        public SysUser? User { get; set; }

        [ForeignKey("RoleId")]
        public SysRole? Role { get; set; }
    }

    // ==========================================
    // 基础档案模块
    // ==========================================

    [Table("base_category")]
    public class BaseCategory
    {
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [Column("parent_id")]
        public int? ParentId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("category_code")]
        public string CategoryCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [Column("sort_order")]
        public int SortOrder { get; set; } = 0;

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    [Table("base_supplier")]
    public class BaseSupplier
    {
        [Key]
        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("supplier_code")]
        public string SupplierCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("supplier_name")]
        public string SupplierName { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("contact_person")]
        public string? ContactPerson { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(200)]
        [Column("address")]
        public string? Address { get; set; }

        [Column("status")]
        public byte Status { get; set; } = 1;

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    [Table("base_customer")]
    public class BaseCustomer
    {
        [Key]
        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("customer_code")]
        public string CustomerCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("customer_name")]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("contact_person")]
        public string? ContactPerson { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(200)]
        [Column("address")]
        public string? Address { get; set; }

        [Column("credit_limit")]
        public decimal CreditLimit { get; set; } = 0;

        [Column("status")]
        public byte Status { get; set; } = 1;

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    [Table("base_goods")]
    public class BaseGoods
    {
        [Key]
        [Column("goods_id")]
        public int GoodsId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("goods_code")]
        public string GoodsCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("goods_name")]
        public string GoodsName { get; set; } = string.Empty;

        [Column("category_id")]
        public int CategoryId { get; set; }

        [MaxLength(50)]
        [Column("barcode")]
        public string? Barcode { get; set; }

        [MaxLength(100)]
        [Column("specification")]
        public string? Specification { get; set; }

        [MaxLength(20)]
        [Column("unit")]
        public string Unit { get; set; } = "件";

        [Column("cost_price")]
        public decimal CostPrice { get; set; } = 0;

        [Column("retail_price")]
        public decimal RetailPrice { get; set; } = 0;

        [Column("min_stock_alert")]
        public int MinStockAlert { get; set; } = 10;

        [Column("max_stock_alert")]
        public int MaxStockAlert { get; set; } = 1000;

        [Column("status")]
        public byte Status { get; set; } = 1;

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [ForeignKey("CategoryId")]
        public BaseCategory? Category { get; set; }

        public WmsInventory? Inventory { get; set; }
    }

    // ==========================================
    // 进销存与仓储业务模块
    // ==========================================

    [Table("wms_inventory")]
    public class WmsInventory
    {
        [Key]
        [Column("inventory_id")]
        public int InventoryId { get; set; }

        [Column("goods_id")]
        public int GoodsId { get; set; }

        [Column("current_stock")]
        public int CurrentStock { get; set; } = 0;

        [Column("last_inbound_time")]
        public DateTime? LastInboundTime { get; set; }

        [Column("last_outbound_time")]
        public DateTime? LastOutboundTime { get; set; }

        [Column("updated_time")]
        public DateTime UpdatedTime { get; set; } = DateTime.Now;

        [ForeignKey("GoodsId")]
        public BaseGoods? Goods { get; set; }
    }

    [Table("pur_order")]
    public class PurOrder
    {
        [Key]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("order_no")]
        public string OrderNo { get; set; } = string.Empty;

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("purchaser_id")]
        public int PurchaserId { get; set; }

        [Column("warehouse_keeper_id")]
        public int? WarehouseKeeperId { get; set; }

        [Column("total_amount")]
        public decimal TotalAmount { get; set; } = 0;

        [Column("order_status")]
        public byte OrderStatus { get; set; } = 0; // 0: 待入库, 1: 已入库, 2: 已取消

        [MaxLength(500)]
        [Column("remark")]
        public string? Remark { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column("inbound_date")]
        public DateTime? InboundDate { get; set; }

        [ForeignKey("SupplierId")]
        public BaseSupplier? Supplier { get; set; }

        [ForeignKey("PurchaserId")]
        public SysUser? Purchaser { get; set; }

        [ForeignKey("WarehouseKeeperId")]
        public SysUser? WarehouseKeeper { get; set; }

        public List<PurOrderDetail> Details { get; set; } = new();
    }

    [Table("pur_order_detail")]
    public class PurOrderDetail
    {
        [Key]
        [Column("detail_id")]
        public int DetailId { get; set; }

        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("goods_id")]
        public int GoodsId { get; set; }

        [Column("purchase_price")]
        public decimal PurchasePrice { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [ForeignKey("OrderId")]
        public PurOrder? Order { get; set; }

        [ForeignKey("GoodsId")]
        public BaseGoods? Goods { get; set; }
    }

    [Table("sale_order")]
    public class SaleOrder
    {
        [Key]
        [Column("order_id")]
        public int OrderId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("order_no")]
        public string OrderNo { get; set; } = string.Empty;

        [Column("customer_id")]
        public int CustomerId { get; set; }

        [Column("saler_id")]
        public int SalerId { get; set; }

        [Column("warehouse_keeper_id")]
        public int? WarehouseKeeperId { get; set; }

        [Column("total_amount")]
        public decimal TotalAmount { get; set; } = 0;

        [Column("order_status")]
        public byte OrderStatus { get; set; } = 0; // 0: 待出库, 1: 已出库, 2: 已作废

        [MaxLength(500)]
        [Column("remark")]
        public string? Remark { get; set; }

        [Column("order_date")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column("outbound_date")]
        public DateTime? OutboundDate { get; set; }

        [ForeignKey("CustomerId")]
        public BaseCustomer? Customer { get; set; }

        [ForeignKey("SalerId")]
        public SysUser? Saler { get; set; }

        [ForeignKey("WarehouseKeeperId")]
        public SysUser? WarehouseKeeper { get; set; }

        public List<SaleOrderDetail> Details { get; set; } = new();
    }

    [Table("sale_order_detail")]
    public class SaleOrderDetail
    {
        [Key]
        [Column("detail_id")]
        public int DetailId { get; set; }

        [Column("order_id")]
        public int OrderId { get; set; }

        [Column("goods_id")]
        public int GoodsId { get; set; }

        [Column("sale_price")]
        public decimal SalePrice { get; set; }

        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [ForeignKey("OrderId")]
        public SaleOrder? Order { get; set; }

        [ForeignKey("GoodsId")]
        public BaseGoods? Goods { get; set; }
    }

    [Table("wms_stock_log")]
    public class WmsStockLog
    {
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }

        [Column("goods_id")]
        public int GoodsId { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("change_type")]
        public string ChangeType { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("related_order_no")]
        public string RelatedOrderNo { get; set; } = string.Empty;

        [Column("before_stock")]
        public int BeforeStock { get; set; }

        [Column("change_qty")]
        public int ChangeQty { get; set; }

        [Column("after_stock")]
        public int AfterStock { get; set; }

        [Column("operator_id")]
        public int OperatorId { get; set; }

        [Column("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [ForeignKey("GoodsId")]
        public BaseGoods? Goods { get; set; }

        [ForeignKey("OperatorId")]
        public SysUser? Operator { get; set; }
    }
}
