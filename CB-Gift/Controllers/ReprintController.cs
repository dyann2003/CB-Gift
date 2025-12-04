using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReprintController : ControllerBase
    {
        private readonly IReprintService _reprintService;

        public ReprintController(IReprintService reprintService)
        {
            _reprintService = reprintService;
        }

        // User submit reprint request
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitReprint([FromBody] ReprintSubmitDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _reprintService.SubmitReprintAsync(dto);
            return Ok(new { message = "Reprint request submitted successfully." });
        }

        // Manager approve
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveReprint([FromBody] ReprintManagerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(managerId))
                return Unauthorized(new { message = "Không tìm thấy thông tin ." });

            await _reprintService.ApproveReprintAsync(dto,managerId);
            return Ok(new { message = "Reprint request approved and new order created." });
        }

        // Manager reject
        [HttpPost("reject")]
        public async Task<IActionResult> RejectReprint([FromBody] ReprintManagerDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(managerId))
                return Unauthorized(new { message = "Không tìm thấy thông tin ." });

            await _reprintService.RejectReprintAsync(dto,managerId);
            return Ok(new { message = "Reprint request rejected and order restored to previous status." });
        }
        [HttpPost("request")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> RequestReprint([FromBody] SellerReprintRequestDto request)
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId))
                return Unauthorized(new { message = "Không tìm thấy thông tin người bán." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _reprintService.RequestReprintAsync(request, sellerId);
                return Ok(new { message = "Yêu cầu in lại đã được gửi đi thành công. Vui lòng chờ Staff duyệt." });
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
                // Log exception chi tiết
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống khi xử lý yêu cầu in lại.", detail = ex.Message });
            }
        }
        [HttpGet("reprint-requests-paginated")]
        [Authorize(Roles = "Staff,Manager")]
        public async Task<IActionResult> GetReprintRequestsPaginated(
        [FromQuery] string? searchTerm,
        [FromQuery] string? filterType,
        [FromQuery] string? sellerIdFilter,
        [FromQuery] string? statusFilter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(staffId))
                return Unauthorized(new { message = "Không tìm thấy thông tin." });

            if (page < 1 || pageSize < 1)
                return BadRequest(new { message = "Tham số phân trang không hợp lệ." });

            try
            {
                var result = await _reprintService.GetReviewReprintRequestsPaginatedAsync(
                    staffId,
                    searchTerm,
                    filterType,
                    sellerIdFilter,
                    statusFilter,
                    page,
                    pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting list of reprint requests.", detail = ex.Message });
            }
        }
        [HttpGet("{reprintId}/details")]
        [Authorize(Roles = "Staff,Manager")]
        public async Task<IActionResult> GetReprintDetails(int reprintId)
        {
            try
            {
                var result = await _reprintService.GetReprintDetailsAsync(reprintId);

                if (result == null)
                {
                    return NotFound(new { message = $"Reprint request with ID {reprintId} not found." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error getting reprint details.", detail = ex.Message });
            }
        }
    }
}
