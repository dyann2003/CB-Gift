using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
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
    }
}
