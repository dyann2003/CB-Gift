using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Authorize(Roles = "Staff,Manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class PlanController : ControllerBase
    {
        private readonly IPlanService _planService;

        public PlanController(IPlanService planService)
        {
            _planService = planService;
        }

        /// Test API: gom đơn ngay lập tức
        [HttpPost("group-submitted")]
        public async Task<IActionResult> GroupSubmittedOrders()
        {
            await _planService.GroupSubmittedOrdersAsync("system");
            return Ok("Orders grouped into plans successfully.");
        }

        /// Lấy kế hoạch sản xuất theo CategoryId và ngày tạo Plan
        [HttpGet("staff-view")]
        public async Task<IActionResult> GetPlansForStaffView([FromQuery] int? categoryId, [FromQuery] DateTime? selectedDate, [FromQuery] string status)
        {
            var result = await _planService.GetPlansForStaffViewAsync(categoryId, selectedDate, status);
            return Ok(result);
        }

        [HttpPut("update-status/{planDetailId}")]
        public async Task<IActionResult> UpdateStatus(int planDetailId, [FromQuery] int newStatus)
        {
            // Kiểm tra giá trị newStatus hợp lệ
            if (newStatus < 6 || newStatus > 8)
            {
                return BadRequest("Trạng thái mới không hợp lệ. Chỉ chấp nhận các giá trị 6, 7, hoặc 8.");
            }

            try
            {
                var success = await _planService.UpdatePlanDetailStatusAsync(planDetailId, newStatus);

                if (!success)
                {
                    return NotFound($"Không tìm thấy PlanDetail với ID: {planDetailId}.");
                }

                return Ok(new { message = "Cập nhật trạng thái thành công." });
            }
            catch (System.Exception ex)
            {
                // Ghi log lỗi ở đây (ví dụ: dùng ILogger)
                return StatusCode(500, "Đã xảy ra lỗi trong quá trình xử lý yêu cầu.");
            }
        }
    }
}
