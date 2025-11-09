using CB_Gift.DTOs;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Route("api/manager/assignments")]
[ApiController]
[Authorize(Roles = "Manager")] // Chỉ Manager mới có quyền
public class DesignerSellerController : ControllerBase
{
    private readonly IDesignerSellerService _service;

    public DesignerSellerController(IDesignerSellerService service)
    {
        _service = service;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllAssignments(
       [FromQuery] string? search = null,
       [FromQuery] string? sellerName = null,
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 10)
    {
        try
        {
            var (data, total) = await _service.GetAllAssignmentsPagedAsync(search, sellerName, page, pageSize);

            if (!data.Any())
                return NotFound(new { message = "Không có mối quan hệ nào giữa Seller và Designer." });

            return Ok(new
            {
                total,
                page,
                pageSize,
                items = data
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách Seller–Designer.", error = ex.Message });
        }
    }



    [HttpPost]
    public async Task<IActionResult> AssignDesigner([FromBody] AssignDesignerDto dto)
    {
        var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Lấy ID của manager đang đăng nhập
        var success = await _service.AssignDesignerToSellerAsync(dto, managerId);
        if (!success) return BadRequest("Gán thất bại.");
        return Ok("Gán Designer cho Seller thành công.");
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveAssignment([FromBody] AssignDesignerDto dto)
    {
        var success = await _service.RemoveDesignerFromSellerAsync(dto);
        if (!success) return NotFound("Không tìm thấy mối quan hệ để xóa.");
        return NoContent();
    }

    [HttpGet("seller/{sellerId}")]
    public async Task<IActionResult> GetDesignersForSeller(string sellerId)
    {
        var result = await _service.GetDesignersForSellerAsync(sellerId);
        return Ok(result);
    }

  

}