using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text; // Thêm
using System.IO;
using Newtonsoft.Json.Linq; // Thêm

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<PaymentController> _logger;

        // Inject IInvoiceService (vì nó chứa logic thanh toán đã refactor)
        public PaymentController(IInvoiceService invoiceService, ILogger<PaymentController> logger)
        {
            _invoiceService = invoiceService;
            _logger = logger;
        }

        /// <summary>
        /// [Seller] Tạo link thanh toán cho một hóa đơn.
        /// </summary>
        [HttpPost("create-link")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkRequest request)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerId))
                    return Unauthorized();

                // Gọi hàm đã refactor trong InvoiceService
                var paymentLink = await _invoiceService.CreatePaymentLinkAsync(request, sellerId);
                return Ok(new { paymentUrl = paymentLink });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo payment link cho InvoiceId {InvoiceId}", request.InvoiceId);
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo link thanh toán." });
            }
        }

        /// <summary>
        /// [Public] Webhook/IPN cho PayOS.
        /// </summary>
        [HttpPost("payos-webhook")]
        [AllowAnonymous] // Webhook phải được phép truy cập công khai
        public async Task<IActionResult> PayOsWebhook()
        {
            /*  string signature = Request.Headers["PayOS-Signature"];
              string payload;
              using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
              {
                  payload = await reader.ReadToEndAsync();
              }

              _logger.LogInformation("--- PayOS Webhook Received ---");
              _logger.LogInformation("Payload: {Payload}", payload);

              try
              {
                  // 1. Log lại
                  int logId = await _invoiceService.LogWebhookAsync("PayOS", payload, signature);

                  // 2. Yêu cầu Service xử lý (không cần await, chạy ngầm)
                  // Dùng _ = để nó chạy trong background và trả về OK ngay
                  _ = _invoiceService.ProcessWebhookPaymentAsync(logId, "PAYOS");

                  // 3. Trả về OK ngay lập tức cho PayOS
                  return Ok();
              }
              catch (Exception ex)
              {
                  _logger.LogError(ex, "Lỗi nghiêm trọng khi ghi log PayOS webhook.");
                  // Nếu ngay cả việc ghi log cũng lỗi, trả về 500
                  return StatusCode(500, new { message = "Lỗi máy chủ khi xử lý webhook." });
              }*/
            // Luôn bật buffering để đọc body nhiều lần nếu cần
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawPayload = await reader.ReadToEndAsync();
            Request.Body.Position = 0; // Reset lại stream cho các lần đọc sau

            // === BƯỚC KIỂM TRA QUAN TRỌNG ===
            try
            {
                var jsonPayload = JObject.Parse(rawPayload);
                // Kiểm tra xem có trường "data" hay không. Nếu không có, đây là request xác thực.
                if (jsonPayload["data"] == null)
                {
                    // Ghi log là đã nhận được request xác thực (tùy chọn)
                    Console.WriteLine("Received PayOS webhook verification request. Responding OK.");
                    // Trả về 200 OK để báo cho PayOS biết URL đã hoạt động
                    return Ok();
                }
            }
            catch
            {
                // Nếu payload không phải là JSON hợp lệ, bỏ qua
                return BadRequest("Invalid payload.");
            }
            // ================================

            // Nếu có trường "data", tiếp tục xử lý như một webhook thanh toán bình thường
            Request.Headers.TryGetValue("x-payos-signature", out var signature);

            try
            {
                int logId = await _invoiceService.LogWebhookAsync("PayOS", rawPayload, signature.ToString());
                await _invoiceService.ProcessWebhookPaymentAsync(logId,"PAYOS");
                return Ok();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chi tiết ở đây
                return StatusCode(500, new { message = "Error processing webhook.", error = ex.Message });
            }
        }

        /// <summary>
        /// [Public] Webhook/IPN cho VNPay.
        /// </summary>
        [HttpGet("vnpay-ipn")]
        [AllowAnonymous] // Webhook phải được phép truy cập công khai
        public async Task<IActionResult> VnPayIpn()
        {
            string signature = Request.Query["vnp_SecureHash"];
            string payload = Request.QueryString.ToString(); // Lấy toàn bộ query string

            _logger.LogInformation("--- VNPay IPN Received ---");
            _logger.LogInformation("QueryString: {Payload}", payload);

            try
            {
                // 1. Log lại
                int logId = await _invoiceService.LogWebhookAsync("VNPay", payload, signature);

                // 2. Yêu cầu Service xử lý (không cần await)
                await _invoiceService.ProcessWebhookPaymentAsync(logId, "VNPAY");

                // 3. Trả về thông báo cho VNPay
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi ghi log VNPay IPN.");
                // Nếu lỗi, VNPay sẽ thử lại, nên trả về lỗi
                return Ok(new { RspCode = "99", Message = "Unknown error" });
            }
        }
    }
}