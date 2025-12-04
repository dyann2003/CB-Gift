using CB_Gift.Models;
using CB_Gift.Services.Payments;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;

namespace CB_Gift.Services.Payments
{
    public class PayOSService : IPaymentGateway
    {
        private readonly PayOS _payOS;

        public PayOSService(IConfiguration configuration)
        {
            // Logic khởi tạo SDK PayOS nằm ở đây
            _payOS = new PayOS(
                configuration["PayOS:ClientId"],
                configuration["PayOS:ApiKey"],
                configuration["PayOS:ChecksumKey"]
            );
        }

        // 1. LOGIC TẠO LINK Payment
        public async Task<string> CreatePaymentLinkAsync(Payment payment, string description, string returnUrl, string cancelUrl, HttpContext httpContext)
        {
            var items = new List<ItemData>
            {
                new ItemData(
                    name: description,
                    quantity: 1,
                    price: (int)payment.Amount
                )
            };

            var paymentData = new PaymentData(
                orderCode: payment.PaymentId, // Rất quan trọng: dùng PaymentId
                amount: (int)payment.Amount,
                description: description.Substring(0, Math.Min(25, description.Length)),
                items: items,
                cancelUrl: cancelUrl,
                returnUrl: returnUrl
            );

            CreatePaymentResult result = await _payOS.createPaymentLink(paymentData);
            return result.checkoutUrl;
        }

        // 2. LOGIC XÁC THỰC WEBHOOK
        public Task<PaymentProcessingResult> VerifyWebhookAsync(string rawPayload, string signature)
        {
            var result = new PaymentProcessingResult { RawResponse = rawPayload };
            try
            {
                var payload = JsonConvert.DeserializeObject<WebhookType>(rawPayload);
                WebhookData verifiedData = _payOS.verifyPaymentWebhookData(payload);

                if (verifiedData?.desc?.Equals("success", StringComparison.OrdinalIgnoreCase) == true ||
                    verifiedData?.desc?.Equals("Thành công", StringComparison.OrdinalIgnoreCase) == true)
                {
                    result.IsSuccess = true;
                    result.PaymentId = (int)verifiedData.orderCode; // Lấy PaymentId
                    result.TransactionId = verifiedData.reference;
                    result.AmountPaid = verifiedData.amount;
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = $"Transaction description: {verifiedData?.desc}";
                    if (verifiedData.orderCode > 0)
                    {
                        result.PaymentId = (int)verifiedData.orderCode;
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
            }

            return Task.FromResult(result);
        }
    }
}