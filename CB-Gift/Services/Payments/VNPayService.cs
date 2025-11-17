using CB_Gift.Models;
using CB_Gift.Utils; // Import helper
using System.Net;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;

namespace CB_Gift.Services.Payments
{
    public class VNPayService : IPaymentGateway
    {
        private readonly IConfiguration _config;
        private readonly VnPayHelper _helper; 
        private readonly ILogger<VNPayService> _logger; 
        public VNPayService(IConfiguration configuration, VnPayHelper helper, ILogger<VNPayService> logger)
        {
            _config = configuration;
            _helper = helper;
            _logger = logger;
        }

        public Task<string> CreatePaymentLinkAsync(Payment payment, string description, string returnUrl, string cancelUrl, HttpContext httpContext)
        {
            var vnp_TmnCode = _config["VNPay:TmnCode"];
            var vnp_HashSecret = _config["VNPay:HashSecret"];
            var vnp_Url = _config["VNPay:BaseUrl"];
            var vnp_Version = _config["VNPay:Version"];

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1") ipAddress = "127.0.0.1";

            // === 1. SỬA LỖI MÚI GIỜ (THEO LOGIC CODE MẪU) ===
            // Code mẫu dùng DateTime.Now (GMT+7). Code của bạn dùng UTC.
            // Chúng ta phải chuyển UTC sang GMT+7.
            DateTime vnpCreateDate_GMT7 = payment.PaymentDate.AddHours(7); // Chuyển sang GMT+7
            DateTime vnpExpireDate_GMT7 = vnpCreateDate_GMT7.AddMinutes(15); // Hết hạn sau 15 phút (tính bằng GMT+7)

            // 2. SỬA LỖI TIẾNG VIỆT ===
            string cleanDescription = StringHelper.RemoveDiacritics(description);
            if (cleanDescription.Length > 250)
            {
                cleanDescription = cleanDescription.Substring(0, 250);
            }
            _helper.ClearRequestData(); 
            _helper.AddRequestData("vnp_Amount", ((long)(payment.Amount * 100)).ToString());
            _helper.AddRequestData("vnp_Command", "pay");
            _helper.AddRequestData("vnp_CreateDate", vnpCreateDate_GMT7.ToString("yyyyMMddHHmmss"));
            _helper.AddRequestData("vnp_CurrCode", "VND");
            _helper.AddRequestData("vnp_IpAddr", ipAddress);
            _helper.AddRequestData("vnp_Locale", "vn");
            _helper.AddRequestData("vnp_OrderInfo", cleanDescription);
            _helper.AddRequestData("vnp_OrderType", "other"); 
            _helper.AddRequestData("vnp_ReturnUrl", returnUrl);
            _helper.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            _helper.AddRequestData("vnp_TxnRef", payment.PaymentId.ToString());
            _helper.AddRequestData("vnp_Version", vnp_Version);
            // [SỬA] Dùng vnpExpireDate_GMT7
            _helper.AddRequestData("vnp_ExpireDate", vnpExpireDate_GMT7.ToString("yyyyMMddHHmmss"));

            string paymentUrl = _helper.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            Console.WriteLine("paymentUrl" + paymentUrl);
            return Task.FromResult(paymentUrl);
        }

        public Task<PaymentProcessingResult> VerifyWebhookAsync(string rawPayload, string signature)
        {
            var result = new PaymentProcessingResult { RawResponse = rawPayload };
            string vnp_HashSecret = _config["VNPay:HashSecret"];
            /*string qs = rawPayload.Substring(rawPayload.IndexOf("?") + 1);
            bool isValidSignature = _helper.ValidateSignatureFromQueryString(signature, vnp_HashSecret, qs);*/

            bool isValidSignature = _helper.ValidateSignatureFromQueryString(signature, vnp_HashSecret, rawPayload);

            if (!isValidSignature)
            {
                result.IsSuccess = false;
                result.Message = "Invalid Signature";
                return Task.FromResult(result);
            }

            var queryParams = System.Web.HttpUtility.ParseQueryString(rawPayload);
            string vnp_ResponseCode = queryParams["vnp_ResponseCode"];
            string vnp_TransactionStatus = queryParams["vnp_TransactionStatus"];

            if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
            {
                result.IsSuccess = true;
                result.Message = "Success";
                result.PaymentId = int.Parse(queryParams["vnp_TxnRef"]);
                result.TransactionId = queryParams["vnp_TransactionNo"];
                result.AmountPaid = decimal.Parse(queryParams["vnp_Amount"]) / 100;
            }
            else
            {
                result.IsSuccess = false;
                result.Message = $"Failed with code {vnp_ResponseCode}";
            }
            Console.WriteLine("ABC" + result);

            return Task.FromResult(result);
        }
    }
}