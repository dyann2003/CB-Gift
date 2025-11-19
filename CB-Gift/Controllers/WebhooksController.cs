using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using Newtonsoft.Json.Linq;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public WebhooksController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

       /* [HttpPost("payos")]
        public async Task<IActionResult> HandlePayOSWebhook()
        {
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
                await _invoiceService.ProcessPayOSWebhookAsync(logId);
                return Ok();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chi tiết ở đây
                return StatusCode(500, new { message = "Error processing webhook.", error = ex.Message });
            }
        }*/
    }
}
