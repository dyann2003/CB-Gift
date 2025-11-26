using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShippingPrintController : ControllerBase
    {
        private readonly IGhnPrintService _ghnPrintService;
        private readonly ILogger<ShippingPrintController> _logger;

        public ShippingPrintController(IGhnPrintService ghnPrintService, ILogger<ShippingPrintController> logger)
        {
            _ghnPrintService = ghnPrintService;
            _logger = logger;
        }

        //Lấy Link (Dùng nếu muốn mở tab mới)
        [HttpPost("get-link")]
        public async Task<IActionResult> GetPrintLink([FromBody] PrintLinkRequest request)
        {
            if (request == null || request.OrderCodes == null || !request.OrderCodes.Any())
            {
                return BadRequest(new { message = "Danh sách mã vận đơn không được rỗng." });
            }

            try
            {
                string printUrl = await _ghnPrintService.GetPrintUrlAsync(request.OrderCodes, request.Size);
                return Ok(new { url = printUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy link in hàng loạt");
                return BadRequest(new { message = ex.Message });
            }
        }

        //Lấy nội dung HTML (Dùng cho in ngầm / in đè Iframe)
        [HttpPost("get-print-html")]
        public async Task<IActionResult> GetPrintHtml([FromBody] PrintLinkRequest request)
        {
            if (request == null || request.OrderCodes == null || !request.OrderCodes.Any())
            {
                return BadRequest(new { message = "Danh sách mã vận đơn không được rỗng." });
            }

            try
            {
                // Gọi hàm lấy HTML Content
                string html = await _ghnPrintService.GetPrintHtmlContentAsync(request.OrderCodes, request.Size);

                // Trả về HTML content
                return Ok(new { htmlContent = html });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy HTML in hàng loạt");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}