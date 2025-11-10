using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CB_Gift.Services.IService;
using CB_Gift.DTOs;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiStudioController : ControllerBase
    {
        private readonly IAiStudioService _aiService;

        public AiStudioController(IAiStudioService aiService)
        {
            _aiService = aiService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateChibi([FromForm] AiPromptDto req)
        {
            if (req.ImageFile == null)
                return BadRequest(new { message = "Vui lòng chọn ảnh để tạo hình." });

            var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif", "image/bmp", "image/tiff", "image/svg+xml" };
            if (!allowedTypes.Contains(req.ImageFile.ContentType.ToLower()))
                return BadRequest(new { message = $"Định dạng ảnh không được hỗ trợ: {req.ImageFile.ContentType}" });

            const long maxFileSize = 50 * 1024 * 1024;
            if (req.ImageFile.Length > maxFileSize)
                return BadRequest(new { message = "Ảnh vượt quá dung lượng tối đa 50MB." });
            // --- (Kết thúc phần xác thực) ---

            try
            {
                using var ms = new MemoryStream();
                await req.ImageFile.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());

                // ⭐️ TRUYỀN TẤT CẢ DỮ LIỆU SANG SERVICE
                var resultImage = await _aiService.GenerateImageAsync(
                    base64,
                    req.Prompt,
                    req.Style,
                    req.AspectRatio,
                    req.Quality
                );

                return Ok(new { image = resultImage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Có lỗi xảy ra khi xử lý ảnh. Vui lòng thử lại sau.",
                    detail = ex.Message
                });
            }
        }
    }
}
