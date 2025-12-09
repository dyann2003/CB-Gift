using CB_Gift.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json.Serialization;
using CB_Gift.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public string? OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string Phone { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }

        public string Address1 { get; set; }

        public string Zipcode { get; set; }

        public string ShipState { get; set; }

        public string ShipCity { get; set; }

        public string ShipCountry { get; set; }
        public string SellerId { get; set; }
        public string? SellerName { get; set; }

        public DateTime CreationDate { get; set; }

        public string? ProductionStatus {  get; set; }
        
        public string? PaymentStatus { get; set; }

        public bool? ActiveTTS { get; set; }

        public decimal? TotalCost { get; set; }

        public string? Tracking {  get; set; }

        public int StatusOrder { get; set; }

        public string? StatusOderName { get; set; }
        public string? Reason { get; set; } // lý do Cancel, lý do Refund
        public string? RejectionReason { get; set; } // lý do không được đồng ý
        public string? ProofUrl { get; set; }
        public int? LatestRefundId { get; set; }
        public bool? IsRefundPending { get; set; }
        public decimal? RefundAmount { get; set; }

    }
    public class OrderWithDetailsDto : OrderDto
    {
        [JsonPropertyOrder(99)] // đẩy xuống cuối
        public List<OrderDetailDto> Details { get; set; } = new();
    }
    public class OrderDetailDto
    {
        public int OrderDetailID { get; set; }
        public int ProductVariantID { get; set; }
        public string Size { get; set; }
        public string Layer { get; set; }
        public string ProductName { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public decimal? Price { get; set; }
        public ProductionStatus? ProductionStatus { get; set; }
        public string? LinkImg { get; set; }
        public bool NeedDesign { get; set; }
        public string LinkThanksCard { get; set; }

        public string AssignedDesignerUserId { get; set; }
        public DateTime AssignedAt { get; set; }

        public string LinkFileDesign { get; set; }

        public string Accessory { get; set; }

        public string Note { get; set; }

        public ProductionStatus? Status { get; set; }
    }
    public class OrderCreateRequest
    {
        public string? OrderCode { get; set; } = string.Empty;
        public int EndCustomerID { get; set; }
        public decimal? TotalCost { get; set; }

        public string? SellerUserId { get; set; }

        public DateTime? OrderDate { get; set; }

        public Decimal? CostScan { get; set; }

        public bool? ActiveTTS { get; set; } = false;

        public string? Tracking {  get; set; }
        public string? ProductionStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public int? StatusOrder { get; set; }
        //  public List<OrderDetailCreateRequest>? OrderDetails { get; set; }
        public int? ToDistrictId { get; set; }
        public int? ToProvinceId { get; set; }
        public string? ToWardCode { get; set; }

        public OrderCreateRequest()
        {
            CostScan = 1;
        }
    }


    public class OrderDetailCreateRequest
    {
        public int OrderId { get; set; }
        [Required(ErrorMessage = "ProductVariantID must be provided.")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantID must be a positive integer.")]
        public int ProductVariantID { get; set; }
        [Required(ErrorMessage = "Quantity is required.")]
       /* [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 1000.")] */
        public int Quantity { get; set; } = 1;
        public decimal? Price { get; set; }
        public string? LinkImg { get; set; }
        [Required(ErrorMessage = "NeedDesign is required.")]
        public bool NeedDesign { get; set; } = false;
        public string? LinkThanksCard { get; set; }
        public string? LinkDesign {  get; set; }
        public string? Accessory { get; set; }
        public string? Note { get; set; }
        public ProductionStatus ProductionStatus { get; set; } = ProductionStatus.DRAFT;
    }

    public class EndCustomerCreateRequest
    {
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Address1 { get; set; }
        public string? ZipCode { get; set; }
        public string? ShipState { get; set; }
        public string? ShipCity { get; set; }
        public string? ShipCountry { get; set; }
    }
    public class MakeOrderDto
    {
        public EndCustomerCreateRequest CustomerInfo { get; set; }
        public OrderCreateRequest OrderCreate { get; set; }
        public List<OrderDetailCreateRequest>? OrderDetails { get; set; }

    }
    public class MakeOrderResponse
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public decimal TotalCost { get; set; }
        public string CustomerName { get; set; }
        public List<MakeOrderDetailResponse> Details { get; set; } = new();
    }

    public class MakeOrderDetailResponse
    {
        public int ProductVariantID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
    public class EndCustomerUpdateRequest
    {
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Address1 { get; set; }
        public string? ZipCode { get; set; }
        public string? ShipState { get; set; }
        public string? ShipCity { get; set; }
        public string? ShipCountry { get; set; }
        public int? ToProvinceId { get; set; }  // <-- THÊM TRƯỜNG NÀY
        public int? ToDistrictId { get; set; }  // <-- THÊM TRƯỜNG NÀY
        public string? ToWardCode { get; set; } // <-- THÊM TRƯỜNG NÀY
    }
    public class OrderCoreUpdateRequest
    {
        public string? OrderCode { get; set; } = string.Empty;
        public bool? ActiveTTS { get; set; }
        public string? Tracking { get; set; }
        public string? ProductionStatus { get; set; }
        public string? PaymentStatus { get; set; }

        public int? ToProvinceId { get; set; }  // <--- THÊM
        public int? ToDistrictId { get; set; }  // <--- THÊM
        public string? ToWardCode { get; set; } // <--- THÊM

        public decimal? TotalCost { get; set; }

    }
    public class OrderDetailUpdateRequest
    {
        public int OrderDetailID { get; set; } // ID của chi tiết đơn hàng đã tồn tại
        public int ProductVariantID { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal? Price { get; set; }
        public string? LinkImg { get; set; }
        public bool NeedDesign { get; set; } = false;
        public string? LinkThanksCard { get; set; }
        public string? LinkDesign { get; set; }
        public string? Accessory { get; set; }
        public string? Note { get; set; }
        public ProductionStatus? ProductionStatus { get; set; }
    }
    public class OrderUpdateDto 
    {
        public EndCustomerUpdateRequest CustomerInfo { get; set; }
        public OrderCoreUpdateRequest OrderUpdate { get; set; } 
        public List<OrderDetailUpdateRequest> OrderDetailsUpdate { get; set; }
    }

    // Trong file UpdateAddressRequest.cs (hoặc DTOs/UpdateAddressRequest.cs)

    public class UpdateAddressRequest
    {
        // Các trường tên/thông tin liên hệ mà FE đang gửi
        public string Name { get; set; }
        public string Phone { get; set; }

        // ✅ THÊM CÁC TRƯỜNG BỊ THIẾU
        public string Email { get; set; }     // <-- Bị thiếu
        public string Address { get; set; }   // <-- Bị thiếu
        public string Address1 { get; set; }

        // Tên địa lý
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }

        // ID địa lý (Cho bảng Order)
        public int? ToProvinceId { get; set; }
        public int? ToDistrictId { get; set; }
        public string? ToWardCode { get; set; }
    }

    public class OrderActivityDto
    {
        // Dữ liệu cơ bản cho timeline
        public DateTime? CreationDate { get; set; }
        public DateTime? OrderDate { get; set; } // Ngày confirmed
        public DateTime? ShippedDate { get; set; }  // ⬇️ Thời gian shipped (delivered)
        // --- Danh sách YÊU CẦU REFUND đã tổng hợp ---
        // Sử dụng RefundDetailsDto (chứa CreatedAt, ReviewedAt, Status, Items)
        public List<RefundDetailsDto> AllRefunds { get; set; } = new List<RefundDetailsDto>();

        // --- Danh sách YÊU CẦU REPRINT đã tổng hợp ---
        // Sử dụng ReprintDetailsDto (chứa RequestDate, Status, RequestedItems)
        public List<ReprintDetailsDto> AllReprints { get; set; } = new List<ReprintDetailsDto>();

        // Dữ liệu cho Order Detail (cần thiết để lấy tên sản phẩm cho mô tả Reprint/Refund nếu không dùng AllReprints/AllRefunds)
        // Tuy nhiên, vì bạn đã dùng AllRefunds/AllReprints, trường này chỉ cần thiết nếu bạn muốn kiểm tra dữ liệu Order Detail cơ bản.
        // Tôi sẽ giữ nó ở dạng List ID để làm nhẹ DTO nhất có thể, hoặc bỏ qua nếu không cần.
    }
}
