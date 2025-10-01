using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CB_Gift.DTOs
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public string? OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
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
        public List<OrderDetailDto> Details { get; set; } = new();
    }
    public class OrderDetailDto
    {
        public int OrderDetailID { get; set; }
        public int ProductVariantID { get; set; }
        public int Quantity { get; set; }
        public decimal? Price { get; set; }
        public string? LinkImg { get; set; }
        public bool NeedDesign { get; set; }
    }
    public class OrderCreateRequest
    {
        public string OrderCode { get; set; } = string.Empty;
        public int EndCustomerID { get; set; }
        public decimal? TotalCost { get; set; }
        public List<OrderDetailCreateRequest>? OrderDetails { get; set; }
    }


    public class OrderDetailCreateRequest
    {
        public int ProductVariantID { get; set; }
        public int Quantity { get; set; }
        public decimal? Price { get; set; }
        public string? LinkImg { get; set; }
        public bool NeedDesign { get; set; }
    }
}
