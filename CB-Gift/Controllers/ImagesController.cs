using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/images")]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        private readonly IImageManagementService _imageService;

        public ImagesController(IImageManagementService imageService)
        {
            _imageService = imageService;
        }

        /// Tải lên một ảnh cho người dùng đang đăng nhập.
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] FileUploadDto uploadDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Không thể xác định người dùng từ token." });
                }

                if (uploadDto.File == null || uploadDto.File.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn một tệp." });
                }

                // Validate định dạng file hình ảnh
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(uploadDto.File.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new
                    {
                        message = $"Định dạng file không hợp lệ. Chỉ chấp nhận các file ảnh: {string.Join(", ", allowedExtensions)}"
                    });
                }

                // Giới hạn kích thước (ví dụ 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5 MB
                if (uploadDto.File.Length > maxFileSize)
                {
                    return BadRequest(new { message = "Kích thước file vượt quá giới hạn cho phép (tối đa 5MB)." });
                }

                await using var stream = uploadDto.File.OpenReadStream();

                var result = await _imageService.UploadImageForUserAsync(stream, uploadDto.File.FileName, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                Exception currentEx = ex;
                while (currentEx.InnerException != null)
                {
                    currentEx = currentEx.InnerException;
                }
                var rootErrorMessage = currentEx.Message;
                Console.WriteLine($"---> DATABASE SAVE FAILED (ROOT): {rootErrorMessage}");

                return StatusCode(500, new { message = "Lỗi khi lưu dữ liệu vào database.", error = rootErrorMessage });
            }
        }

        /// Lấy danh sách tất cả các ảnh đã được upload bởi người dùng đang đăng nhập.
        [HttpGet("my-images")]
        public async Task<IActionResult> GetMyImages()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Không thể xác định người dùng." });
            }

            var images = await _imageService.GetImagesByUserAsync(userId);
            return Ok(images);
        }
    }
}
