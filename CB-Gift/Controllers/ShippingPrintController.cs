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

        [HttpPost("get-link")]
        public async Task<IActionResult> GetPrintLink([FromBody] PrintLinkRequest request)
        {
            if (request == null || request.OrderCodes == null || !request.OrderCodes.Any())
            {
                return BadRequest(new { message = "Danh sách mã vận đơn không được rỗng." });
            }

            try
            {
                // Yêu cầu service lấy link với cả danh sách
                string printUrl = await _ghnPrintService.GetPrintUrlAsync(request.OrderCodes, request.Size);

                // Trả link về cho React
                return Ok(new { url = printUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy link in hàng loạt");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
