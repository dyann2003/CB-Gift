using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

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
                    return Unauthorized(new { message = "The user cannot be identified." });
                }

                if (uploadDto.File == null || uploadDto.File.Length == 0)
                {
                    return BadRequest(new { message = "Please select a file." });
                }
                if (!IsValidFileName(uploadDto.File.FileName))
                {
                    return BadRequest(new
                    {
                        message = "File name contains invalid characters. Only letters, numbers, '_', '-', '.' are allowed."
                    });
                }

                // Validate định dạng file hình ảnh
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(uploadDto.File.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new
                    {
                        message = $"Invalid file format. Only image files are accepted: {string.Join(", ", allowedExtensions)}"
                    });
                }

                // Giới hạn kích thước (ví dụ 5MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10 MB
                if (uploadDto.File.Length > maxFileSize)
                {
                    return BadRequest(new { message = "The file size exceeds the allowed limit (maximum 10MB)." });
                }

                await using var stream = uploadDto.File.OpenReadStream();

                var result = await _imageService.UploadImageForUserAsync(stream, uploadDto.File.FileName, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                Exception currentEx = ex;
                while (currentEx.InnerException != null)
                    currentEx = currentEx.InnerException;

                var rootErrorMessage = currentEx.Message;

                // log nội bộ
                Console.WriteLine($"[UPLOAD IMAGE ERROR]: {rootErrorMessage}");

                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while uploading the file.",
                    detail = rootErrorMessage
                });
            }

        }

        /// Lấy danh sách tất cả các ảnh đã được upload bởi người dùng đang đăng nhập.
        [HttpGet("my-images")]
        public async Task<IActionResult> GetMyImages()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "The user cannot be identified." });
            }

            var images = await _imageService.GetImagesByUserAsync(userId);
            return Ok(images);
        }
        // === ENDPOINT MỚI (UPLOAD CẢ ẢNH VÀ VIDEO) ===
        [HttpPost("upload-media")]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB Limit
        public async Task<IActionResult> UploadMedia([FromForm] FileUploadDto uploadDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "The user cannot be identified." });

                if (uploadDto.File == null || uploadDto.File.Length == 0)
                    return BadRequest(new { message = "Please select a file." });
                if (!IsValidFileName(uploadDto.File.FileName))
                {
                    return BadRequest(new
                    {
                        message = "File name contains invalid characters. Only letters, numbers, '_', '-', '.' are allowed."
                    });
                }
                // 1. Validate loại file (cho phép cả ảnh và video)
                var allowedExtensions = new[] {
                    ".jpg", ".jpeg", ".png", ".gif", ".webp", // Ảnh
                    ".mp4", ".mov", ".avi", ".wmv" // Video
                };
                var fileExtension = Path.GetExtension(uploadDto.File.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { message = $"Invalid file format. Only accept: {string.Join(", ", allowedExtensions)}" });

                // 2. Validate kích thước (100MB)
                const long maxFileSize = 100 * 1024 * 1024; // 200 MB
                if (uploadDto.File.Length > maxFileSize)
                    return BadRequest(new { message = "The file size has exceeded the limit (100MB)." });

                await using var stream = uploadDto.File.OpenReadStream();

                // 3. GỌI SERVICE MỚI
                var result = await _imageService.UploadMediaForUserAsync(
                    stream,
                    uploadDto.File.FileName,
                    userId,
                    uploadDto.File.ContentType // <-- Truyền ContentType vào
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                Exception currentEx = ex;
                while (currentEx.InnerException != null) currentEx = currentEx.InnerException;
                var rootErrorMessage = currentEx.Message;

                return StatusCode(500, new { message = "Error uploading media.", error = rootErrorMessage });
            }
        }
        private bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var name = Path.GetFileNameWithoutExtension(fileName);

            return Regex.IsMatch(name, @"^[\p{L}0-9 _\-\.]+$");
        }


    }
}
