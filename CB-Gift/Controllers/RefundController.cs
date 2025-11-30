// File: Controllers/RefundController.cs

using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bảo vệ Controller ở cấp độ chung
    public class RefundController : ControllerBase
    {
        private readonly IRefundService _refundService;

        public RefundController(IRefundService refundService)
        {
            _refundService = refundService;
        }

        // ----------------------------------------------------------------------
        // 1. ENDPOINT YÊU CẦU HOÀN TIỀN (SELLER)
        // POST: api/Refund/request
        // ----------------------------------------------------------------------
        [HttpPost("request")]
        [Authorize(Roles = "Seller")] // Chỉ Seller mới được yêu cầu
        public async Task<IActionResult> RequestRefund([FromBody] RefundRequestDto request)
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId))
                return Unauthorized(new { message = "No seller information found." });

            if (request.Items == null || !request.Items.Any())
                return BadRequest(new { message = "Refund requests must select at least one product detail." });

            try
            {
                await _refundService.RequestRefundAsync(request, sellerId);
                return Ok(new { message = "Refund request has been sent successfully. Please wait for Manager approval." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log exception chi tiết
                return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống khi xử lý yêu cầu hoàn tiền.", detail = ex.Message });
            }
        }

        // ----------------------------------------------------------------------
        // 2. ENDPOINT DUYỆT YÊU CẦU HOÀN TIỀN (STAFF/MANAGER)
        // POST: api/Refund/{refundId}/review
        // ----------------------------------------------------------------------
        [HttpPost("{refundId}/review")]
        [Authorize(Roles = "Staff,Manager")] // Chỉ Staff/Manager mới được duyệt
        public async Task<IActionResult> ReviewRefund(int refundId, [FromBody] ReviewRefundDto request)
        {
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(staffId))
                return Unauthorized(new { message = "Không tìm thấy thông tin ." });

            try
            {
                await _refundService.ReviewRefundAsync(refundId, request, staffId);

                string outcome = request.Approved ? "Approved" : "Reject";
                return Ok(new { message = $"{outcome} refund request #{refundId} successful." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log exception chi tiết
                return StatusCode(500, new { message = "A system error occurred while processing the approval request.", detail = ex.Message });
            }
        }

        // ----------------------------------------------------------------------
        // 3. ENDPOINT XEM CHI TIẾT YÊU CẦU HOÀN TIỀN (XÁC THỰC QUYỀN)
        // GET: api/Refund/{refundId}/details
        // ----------------------------------------------------------------------
        [HttpGet("{refundId}/details")]
        [Authorize(Roles = "Seller,Staff,Manager")]
        public async Task<IActionResult> GetRefundDetails(int refundId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng." });

            try
            {
                // 1. --- LOGIC XÁC THỰC QUYỀN TRUY CẬP ---

                // Kiểm tra quyền Staff/Manager (Được phép xem mọi thứ)
                if (User.IsInRole("Manager") || User.IsInRole("Staff"))
                {
                    // Staff/Manager có quyền, lấy chi tiết và trả về
                    var details = await _refundService.GetRefundRequestDetailsAsync(refundId);
                    if (details == null) return NotFound(new { message = "Không tìm thấy chi tiết yêu cầu hoàn tiền." });
                    return Ok(details);
                }

                // Kiểm tra quyền Seller
                if (User.IsInRole("Seller"))
                {
                    // Seller chỉ được xem nếu họ là người đã gửi yêu cầu
                    var isRequester = await _refundService.IsUserRequesterAsync(refundId, userId);

                    if (isRequester)
                    {
                        // Seller có quyền, lấy chi tiết và trả về
                        var details = await _refundService.GetRefundRequestDetailsAsync(refundId);
                        if (details == null) return NotFound(new { message = "Không tìm thấy chi tiết yêu cầu hoàn tiền." });
                        return Ok(details);
                    }
                }

                // 2. Nếu không phải Staff/Manager và không phải Seller gửi yêu cầu
                return StatusCode(403, new { message = "Bạn không có quyền truy cập vào chi tiết yêu cầu hoàn tiền này." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log exception chi tiết
                return StatusCode(500, new { message = "Error retrieving refund request details.", detail = ex.Message });
            }
        }
        [HttpGet("pending-requests-paginated")]
        [Authorize(Roles = "Staff,Manager")]
        public async Task<IActionResult> GetPendingRequestsPaginated(
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
                var result = await _refundService.GetReviewRequestsPaginatedAsync(
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
                return StatusCode(500, new { message = "Error getting list of pending requests.", detail = ex.Message });
            }
        }
    }
}