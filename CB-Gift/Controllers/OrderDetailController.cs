using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderDetailController : ControllerBase
    {
        private readonly IOrderDetailService _orderDetailService;

        public OrderDetailController(IOrderDetailService orderDetailService)
        {
            _orderDetailService = orderDetailService;
        }

        [HttpGet("{orderDetailId}")]
        public async Task<IActionResult> GetOrderDetail(int orderDetailId)
        {
            var detail = await _orderDetailService.GetOrderDetailByIdAsync(orderDetailId);

            if (detail == null)
                return NotFound("OrderDetail not found");

            return Ok(detail);
        }

        /*[HttpPut("{orderDetailId}/accept")] // PUT api/OrderDetail/123/accept
        public async Task<IActionResult> AcceptOrderDetail(int orderDetailId)
        {
            var updatedOrderDetail = await _orderDetailService.AcceptOrderDetailAsync(orderDetailId);

            if (updatedOrderDetail == null)
            {
                return NotFound($"Không tìm thấy OrderDetail với ID: {orderDetailId}");
            }

            return NoContent();
        }

        [HttpPut("{orderDetailId}/reject")] // PUT api/OrderDetail/123/reject
        public async Task<IActionResult> RejectOrderDetail(int orderDetailId)
        {
            var updatedOrderDetail = await _orderDetailService.RejectOrderDetailAsync(orderDetailId);

            if (updatedOrderDetail == null)
            {
                return NotFound($"Không tìm thấy OrderDetail với ID: {orderDetailId}");
            }

            return NoContent();
        }*/
        /// <summary>
        /// (QC) Từ chối (reject) một sản phẩm và ghi lại lý do
        /// </summary>
        // API MỚI: Dùng POST, nhận ID từ route và Reason từ body
        [HttpPost("{orderDetailId}/reject")]
        public async Task<IActionResult> RejectDetail(int orderDetailId, [FromBody] QcRejectRequestDto request)
        {
            // 1. Kiểm tra DTO (Reason có bắt buộc không)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 2. Lấy ID của QC đang đăng nhập
            var qcUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(qcUserId))
            {
                return Unauthorized();
            }

            try
            {
                // 3. Gọi service mới (đã bao gồm logic ghi log)
                var updatedDetail = await _orderDetailService.RejectOrderDetailAsync(orderDetailId, request, qcUserId);

                if (updatedDetail == null)
                {
                    return NotFound(new { message = $"Không tìm thấy OrderDetail với ID: {orderDetailId}" });
                }

                // 4. Trả về 200 OK (thay vì 204 NoContent)
                return Ok(new { message = "Sản phẩm đã được đánh dấu 'QC_FAIL' và lý do đã được ghi lại." });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi ex
                return StatusCode(500, new { message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

        /// <summary>
        /// (QC) Chấp nhận (accept) một sản phẩm
        /// </summary>
        [HttpPost("{orderDetailId}/accept")]
        public async Task<IActionResult> AcceptDetail(int orderDetailId)
        {
            var qcUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(qcUserId))
            {
                return Unauthorized();
            }

            try
            {
                var updatedDetail = await _orderDetailService.AcceptOrderDetailAsync(orderDetailId);

                if (updatedDetail == null)
                {
                    return NotFound(new { message = $"Không tìm thấy OrderDetail với ID: {orderDetailId}" });
                }

                return Ok(new { message = "Sản phẩm đã được đánh dấu 'QC_DONE'." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống.", error = ex.Message });
            }
        }

    }
}
