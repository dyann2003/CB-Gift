using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class InvoiceDto
    {
    }
    // DTO để Staff gửi yêu cầu tạo hóa đơn
    public class CreateInvoiceRequest
    {
        [Required]
        public string SellerId { get; set; }

        //Danh sách ID các đơn hàng cần tạo hóa đơn (tùy chọn)
        public List<int>? OrderIds { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? Notes { get; set; }
        public string? DiscountCode { get; set; }
    }
    // DTO để Seller yêu cầu tạo link thanh toán
    public class CreatePaymentLinkRequest
    {
        [Required]
        public int InvoiceId { get; set; }

        // Nếu null: Thanh toán hết số nợ còn lại.
        // Nếu có giá trị: Thanh toán đúng số tiền đó.
        public decimal? Amount { get; set; }
        [Required]
        public string ReturnUrl { get; set; }
        [Required]
        public string CancelUrl { get; set; }
        public string? GatewayName { get; set; } 
    }
    // DTO chứa payload từ webhook của PayOS
    public class PayOSWebhookPayload
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("data")]
        public PayOSWebhookData Data { get; set; } 

        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
    public class PayOSWebhookData
    {
        [JsonProperty("orderCode")]
        public int OrderCode { get; set; } 

        [JsonProperty("amount")]
        public int Amount { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("transactionDateTime")]
        public string TransactionDateTime { get; set; }

        [JsonProperty("desc")]
        public string Desc { get; set; }
    }
    public class SellerReceivablesDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public decimal TotalDebt { get; set; } // Công nợ (Receivables)
        public decimal TotalSales { get; set; } // Tổng doanh số
    }
    public class InvoiceSummaryDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public string SellerUserId { get; set; }
        public string SellerName { get; set; } // Lấy từ join
        public DateTime CreatedAt { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal RemainingBalance => TotalAmount - AmountPaid;
        public string Status { get; set; }
    }
    public class OverdueCheckDto
    {
        // Cho biết có hóa đơn quá hạn hay không
        public bool HasOverdueInvoice { get; set; }

        // ID của hóa đơn quá hạn (để frontend có thể gọi chi tiết)
        public int? OverdueInvoiceId { get; set; }
    }
    // DTO cho tab "Sales History" (danh sách các tháng)
    public class SellerMonthlySalesDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } // "November 2024"
        public decimal TotalAmount { get; set; }
        // Trạng thái của tháng: "Invoiced", "PartiallyInvoiced", "Uninvoiced"
        public string Status { get; set; }
    }
    //huy
    // DTO cho tab "Sales History" (khi mở rộng 1 tháng) 
    public class SellerOrderDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalCost { get; set; }
        public string OrderStatus { get; set; } // Tên trạng thái (vd: "Đã Ship")
    }

    // DTO cho tab "Payment History" (Cập nhật DTO từ lần trước)
    public class PaymentSummaryDto
    {
        public int PaymentId { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; } // "Completed", "Pending", "Failed"
        public string TransactionId { get; set; }

        // [THÊM MỚI] - Thông tin từ Invoice gốc
        public decimal InvoiceTotal { get; set; }
        public decimal InvoiceRemaining { get; set; }
        public string InvoiceStatus { get; set; }
    }

    // DTO cho request tạo hóa đơn theo tháng
    public class CreateMonthlyInvoiceRequest
    {
        public string SellerId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string Notes { get; set; }
    }

}
