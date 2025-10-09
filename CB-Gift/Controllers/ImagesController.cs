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

        // Sử dụng service quản lý ảnh ở tầng cao hơn
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

                await using var stream = uploadDto.File.OpenReadStream();

                var result = await _imageService.UploadImageForUserAsync(stream, uploadDto.File.FileName, userId);

                return Ok(result);
            }
            catch (Exception ex) // SỬA LỖI Ở ĐÂY
            {
                // Tạo một biến để lặp và tìm ra lỗi gốc
                Exception currentEx = ex;
                while (currentEx.InnerException != null)
                {
                    currentEx = currentEx.InnerException;
                }
                // Lấy thông báo lỗi từ exception trong cùng nhất
                var rootErrorMessage = currentEx.Message;

                // Ghi log lỗi chi tiết ra console trên server để bạn xem
                Console.WriteLine($"---> DATABASE SAVE FAILED (ROOT): {rootErrorMessage}");

                // Trả về lỗi chi tiết cho client
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
