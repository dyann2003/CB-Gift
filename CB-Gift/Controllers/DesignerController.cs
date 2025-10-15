using CB_Gift.DTOs;
using CB_Gift.Services.IService; // <-- SỬA LẠI USING CHO ĐÚNG
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

[Route("api/designer/tasks")]
[ApiController]
[Authorize(Roles = "Designer")]
public class DesignerController : ControllerBase
{
    private readonly IDesignerTaskService _designerTaskService;

    public DesignerController(IDesignerTaskService designerTaskService)
    {
        _designerTaskService = designerTaskService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTasks()
    {
        var designerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(designerId)) return Unauthorized();

        var tasks = await _designerTaskService.GetAssignedTasksAsync(designerId);
        return Ok(tasks);
    }

    [HttpPost("{orderDetailId}/upload")]
    public async Task<IActionResult> UploadDesign(int orderDetailId, [FromForm] UploadDesignDto dto)
    {
        try
        {
            var designerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(designerId)) return Unauthorized();

            if (dto.DesignFile == null || dto.DesignFile.Length == 0)
            {
                return BadRequest(new { message = "File thiết kế không được để trống." });
            }

            var success = await _designerTaskService.UploadDesignFileAsync(orderDetailId, designerId, dto);

            if (!success)
            {
                return StatusCode(403, new { message = "Bạn không có quyền thực hiện hoặc đơn hàng không ở trạng thái hợp lệ để upload." });
            }

            return Ok(new { message = "Upload file thiết kế thành công. Đơn hàng đã được chuyển sang trạng thái chờ duyệt." });
        }
        catch (Exception ex)
        {
            // Log lỗi ra console hoặc hệ thống log của bạn
            Console.WriteLine($"ERROR uploading design: {ex.Message}");
            return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn trong quá trình upload." });
        }
    }
    /// <summary>
    /// API để Designer cập nhật trạng thái thiết kế.
    /// PUT: api/designer/tasks/status/{orderDetailId}
    /// </summary>
    [HttpPut("status/{orderDetailId}")]
    public async Task<IActionResult> UpdateDesignStatus(
        int orderDetailId,
        [FromBody] UpdateStatusRequest request)
    {
        try
        {
            // 1. Gọi Service để xử lý logic
            var success = await _designerTaskService.UpdateStatusAsync(orderDetailId, request.OrderStatus);

            if (!success)
            {
                // Nếu Service trả về false (ví dụ: không tìm thấy)
                return NotFound(new { message = $"Order Detail with ID {orderDetailId} not found or status update failed." });
            }

            // 2. Trả về phản hồi thành công (204 No Content)
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            // Xử lý lỗi validation từ Service
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Xử lý lỗi logic nghiệp vụ từ Service
            return StatusCode(403, new { message = ex.Message }); // 403 Forbidden hoặc 400 Bad Request
        }
        catch (Exception ex)
        {
            // Xử lý lỗi server không mong muốn
            return StatusCode(500, new { message = "An internal server error occurred while updating the status." });
        }
    }
}