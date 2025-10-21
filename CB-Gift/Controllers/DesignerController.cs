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
        // Thêm kiểm tra ModelState để bắt lỗi Model Binding (400 Bad Request)
        if (!ModelState.IsValid)
        {
            // TRẢ VỀ LỖI CHI TIẾT ĐỂ XÁC ĐỊNH TRƯỜNG NÀO LỖI
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            // Nếu errors rỗng, lấy tên trường bị lỗi
            if (!errors.Any())
            {
                var invalidFields = ModelState.Keys.Where(k => ModelState[k].Errors.Any());
                errors.Add($"Invalid fields: {string.Join(", ", invalidFields)}");
            }

            return BadRequest(new
            {
                message = "Model binding failed.",
                errors = errors
            });
        }

        try
        {
            var designerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(designerId)) return Unauthorized();

            // KIỂM TRA MỚI: Phải có FILE MỚI hoặc URL CŨ
            if ((dto.DesignFile == null || dto.DesignFile.Length == 0) && string.IsNullOrEmpty(dto.FileUrl))
            {
                return BadRequest(new { message = "Vui lòng cung cấp File thiết kế mới hoặc chọn File từ kho ảnh." });
            }

            var success = await _designerTaskService.UploadDesignFileAsync(orderDetailId, designerId, dto);

            if (!success)
            {
                // Trả về 403 nếu Service trả về false (Designer ID không khớp hoặc OrderDetail không tồn tại)
                return StatusCode(403, new { message = "Bạn không có quyền thực hiện hoặc đơn hàng không ở trạng thái hợp lệ để upload." });
            }

            return Ok(new { message = "Upload file thiết kế thành công. Đơn hàng đã được chuyển sang trạng thái chờ duyệt." });
        }
        catch (InvalidOperationException ex)
        {
            // Lỗi logic nghiệp vụ từ Service
            return StatusCode(403, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR uploading design: {ex.Message}");
            // TRẢ VỀ THÔNG BÁO LỖI SERVER RÕ RÀNG HƠN
            return StatusCode(500, new { message = $"Đã xảy ra lỗi server: {ex.Message}" });
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
            var success = await _designerTaskService.UpdateStatusAsync(orderDetailId, request.ProductionStatus);

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