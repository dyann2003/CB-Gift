using CB_Gift.Models;

namespace CB_Gift.Services.Payments
{
    public class PaymentProcessingResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? InvoiceId { get; set; }
        public int? PaymentId { get; set; }
        public string? TransactionId { get; set; }
        public decimal AmountPaid { get; set; }
        public string RawResponse { get; set; } = string.Empty; // Để lưu log
    }
    public interface IPaymentGateway
    {
        // Hàm tạo link
        Task<string> CreatePaymentLinkAsync(Payment payment, string description, string returnUrl, string cancelUrl, HttpContext httpContext);

        // Hàm xác thực Webhook
        Task<PaymentProcessingResult> VerifyWebhookAsync(string rawPayload, string signature);
    }
}

