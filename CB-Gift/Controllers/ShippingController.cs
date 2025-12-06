using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly IShippingService _shippingService;
        private readonly ILogger<ShippingController> _logger;

        public ShippingController(IShippingService shippingService, ILogger<ShippingController> logger)
        {
            _shippingService = shippingService;
            _logger = logger;

            // Dòng này giúp bạn debug: Khi chạy, nhìn vào Console sẽ thấy nó đang dùng Demo hay Real
            _logger.LogInformation($"Shipping Controller đang sử dụng dịch vụ: {_shippingService.GetType().Name}");
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _shippingService.CreateOrderAsync(request);
                return Ok(new
                {
                    OrderCode = result.OrderCode,
                    TotalFee = result.TotalFee
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi tạo đơn.");
                return StatusCode(500, new { Message = $"Lỗi server nội bộ: {ex.Message}" });
            }
        }

        [HttpGet("track/{orderCode}")]
        public async Task<IActionResult> TrackOrder(string orderCode)
        {
            try
            {
                // Nếu là DemoService -> Logic tự nhảy status
                // Nếu là RealService -> Logic gọi API GHN
                var result = await _shippingService.TrackOrderAsync(orderCode);
                return Ok(new { data = result });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi theo dõi đơn.");
                return StatusCode(500, new { message = "Lỗi server nội bộ." });
            }
        }

        [HttpPost("update-status-manual")]
        public async Task<IActionResult> UpdateStatusManual([FromBody] UpdateTrackingRequest request)
        {
            try
            {
                // Kiểm tra xem service hiện tại có phải là ManualGhnService không
                if (_shippingService is DemoShippingService manualService)
                {
                    await manualService.ManualUpdateStatusAsync(request.OrderCode, request.NewStatus);
                    return Ok(new { Message = $"Đã cập nhật đơn {request.OrderCode} sang trạng thái {request.NewStatus}" });
                }

                return BadRequest(new { Message = "Hệ thống đang chạy chế độ Real, không thể cập nhật thủ công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

    }
}
