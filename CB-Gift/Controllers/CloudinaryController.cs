using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CloudinaryController : ControllerBase
    {
        private readonly ICloudinaryService _cloudinaryService;

        public CloudinaryController(ICloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("upload")]
        // SỬA LỖI: Thay đổi tham số đầu vào để sử dụng DTO
        public async Task<IActionResult> UploadImage([FromForm] FileUploadDto uploadDto)
        {
            try
            {
                // Truy cập file qua đối tượng DTO
                var file = uploadDto.File;

                // Kiểm tra file bên trong DTO
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn một tệp để tải lên." });
                }

                // Gọi Service để tải lên
                var uploadResult = await _cloudinaryService.UploadImageAsync(file);

                return Ok(new
                {
                    message = "Tải lên thành công!",
                    publicId = uploadResult.PublicId,
                    url = uploadResult.SecureUrl.ToString()
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Đã xảy ra lỗi trong quá trình tải lên.", error = ex.Message });
            }
        }
    }
}
