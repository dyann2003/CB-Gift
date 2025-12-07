// File mới: CB_Gift.DTOs/OrderEditDtos.cs
using System.Collections.Generic;
using System;
using CB_Gift.Models.Enums;

namespace CB_Gift.DTOs
{
    // === RESPONSE DTOs (GET /api/Order/edit/{orderId}) ===

    public class ProductVariantDetailDto
    {
        public int ProductVariantId { get; set; }
        public string? Sku { get; set; }
        public string? SizeInch { get; set; }
        public decimal? BaseCost { get; set; }
        public decimal? ShipCost { get; set; }
        public decimal? ExtraShipping { get; set; }
        public decimal? TotalCost { get; set; }
        public string? Layer { get; set; }
        public string? ThicknessMm { get; set; }
        public string? CustomShape { get; set; }
    }

    public class OrderEditDetailDto // Chi tiết sản phẩm đang có trong Order
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public string? LinkImg { get; set; } // Map từ LinkImg/DesignFileUrl 
        public string? LinkThanksCard { get; set; } // Map từ LinkThanksCard
        public string? LinkFileDesign { get; set; } // Map từ LinkFileDesign
        public string? Note { get; set; }
        public int ProductionStatus { get; set; }

        // Chi phí của variant đang chọn (lấy từ Entity OrderDetail sau khi sửa)
        public decimal? BaseCost { get; set; }
        public decimal? ShipCost { get; set; }
        public decimal? ExtraShipping { get; set; }
        public decimal? TotalCostDetail { get; set; }

        // TẤT CẢ Variants của Product cha (QUAN TRỌNG: để FE hiển thị dropdown Size ở Step 2)
        public List<ProductVariantDetailDto> AllProductVariants { get; set; } = new List<ProductVariantDetailDto>();
    }

    public class OrderEditResponseDto // DTO cho việc lấy chi tiết Order để Edit
    {
        public int OrderId { get; set; }
        public string? OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public string SellerUserId { get; set; }
        public bool? ActiveTTS { get; set; }
        public decimal? TotalCost { get; set; }
        public int EndCustomerId { get; set; }

        // Thông tin Khách hàng (Step 1)
        public string? CustomerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Address1 { get; set; }
        public int? ToProvinceId { get; set; }
        public int? ToDistrictId { get; set; }
        public string? ToWardCode { get; set; }
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }

        public List<OrderEditDetailDto> Details { get; set; } = new List<OrderEditDetailDto>();
    }

    // === REQUEST DTOs (PUT /api/Order/edit/{orderId}) ===

    public class EndCustomerEditRequest
    {
        public int EndCustomerId { get; set; }
        public string? Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Address1 { get; set; }
        // Cần truyền các ID địa lý để BE lưu vào Order Entity (cho Shipping)
        public int? ToProvinceId { get; set; }
        public int? ToDistrictId { get; set; }
        public string? ToWardCode { get; set; }
    }

    public class OrderDetailEditRequest
    {
        public int OrderDetailId { get; set; } // > 0: Update detail, = 0: Add new detail
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        // FE sẽ gửi các link đã upload lên (được xử lý trong FE)
        public string? LinkImg { get; set; }
        public string? LinkThanksCard { get; set; }
        public string? LinkFileDesign { get; set; }
        public string? Note { get; set; }
    }

    public class OrderEditRequest
    {
        public EndCustomerEditRequest CustomerInfo { get; set; } = new EndCustomerEditRequest();
        public List<OrderDetailEditRequest> Details { get; set; } = new List<OrderDetailEditRequest>(); // Danh sách ADD/UPDATE
        public List<int> RemovedDetailIds { get; set; } = new List<int>(); // Danh sách ID bị XÓA

        public decimal OrderTotalCost { get; set; } // <<-- TotalCost cuối cùng do FE tính toán
        public bool? ActiveTTS { get; set; }
        public string? Note { get; set; } // Order Note (nếu có)
    }
}