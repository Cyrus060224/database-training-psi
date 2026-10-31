using System.Collections.Generic;

namespace Psi.Api.Models
{
    // 登录请求 DTO
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // 登录响应 DTO
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }

    // 商品查询参数
    public class GoodsQueryParams
    {
        public string? Keyword { get; set; }
        public int? CategoryId { get; set; }
        public bool? AlertOnly { get; set; } // 仅看库存告警商品
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // 创建/编辑商品 DTO
    public class GoodsSaveDto
    {
        public int GoodsId { get; set; }
        public string GoodsCode { get; set; } = string.Empty;
        public string GoodsName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string? Barcode { get; set; }
        public string? Specification { get; set; }
        public string Unit { get; set; } = "件";
        public decimal CostPrice { get; set; }
        public decimal RetailPrice { get; set; }
        public int MinStockAlert { get; set; } = 10;
        public int MaxStockAlert { get; set; } = 1000;
    }

    // 订单明细创建项
    public class OrderItemDto
    {
        public int GoodsId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    // 采购单创建请求
    public class CreatePurOrderDto
    {
        public int SupplierId { get; set; }
        public string? Remark { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    // 销售单创建请求
    public class CreateSaleOrderDto
    {
        public int CustomerId { get; set; }
        public string? Remark { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    // 首页统计看板响应
    public class DashboardStatsDto
    {
        public decimal TotalSaleAmount { get; set; } // 销售总额
        public decimal TotalPurchaseAmount { get; set; } // 采购总额
        public int TotalGoodsCount { get; set; } // 商品总种类
        public int StockAlertCount { get; set; } // 告警商品数量
        public List<TopSellingItemDto> TopSellingGoods { get; set; } = new();
        public List<RecentOrderItemDto> RecentOrders { get; set; } = new();
    }

    public class TopSellingItemDto
    {
        public string GoodsName { get; set; } = string.Empty;
        public int SaleQty { get; set; }
        public decimal SaleAmount { get; set; }
    }

    public class RecentOrderItemDto
    {
        public string OrderNo { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "采购" 或 "销售"
        public string PartnerName { get; set; } = string.Empty; // 客户或供应商名
        public decimal Amount { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
    }
}
