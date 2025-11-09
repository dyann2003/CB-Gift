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
}
