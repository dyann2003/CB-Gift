using CB_Gift.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json.Serialization;

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

        public decimal? TotalCost { get; set; }

        public string? Tracking {  get; set; }

        public int StatusOrder { get; set; }

        public string? StatusOderName { get; set; }

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
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal? Price { get; set; }
        public string? LinkImg { get; set; }
        public bool NeedDesign { get; set; }
        public string LinkThanksCard { get; set; }

        public string AssignedDesignerUserId { get; set; }

        public string LinkFileDesign { get; set; }

        public string Accessory { get; set; }

        public string Note { get; set; }
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
      //  public List<OrderDetailCreateRequest>? OrderDetails { get; set; }


        public OrderCreateRequest()
        {
            CostScan = 1;
        }
    }


    public class OrderDetailCreateRequest
    {
        public int OrderId { get; set; }
        public int ProductVariantID { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal? Price { get; set; }
        public string? LinkImg { get; set; }
        public bool NeedDesign { get; set; } = false;
        public string? LinkThanksCard { get; set; }
        public string? LinkDesign {  get; set; }
        public string? Accessory { get; set; }
        public string? Note { get; set; }
        
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
    }
    public class OrderCoreUpdateRequest
    {
        public string? OrderCode { get; set; } = string.Empty;
        public bool? ActiveTTS { get; set; }
        public string? Tracking { get; set; }
        public string? ProductionStatus { get; set; }
        public string? PaymentStatus { get; set; }
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
    }
    public class OrderUpdateDto 
    {
        public EndCustomerUpdateRequest CustomerInfo { get; set; }
        public OrderCoreUpdateRequest OrderUpdate { get; set; } 
        public List<OrderDetailUpdateRequest> OrderDetailsUpdate { get; set; }
    }
}
