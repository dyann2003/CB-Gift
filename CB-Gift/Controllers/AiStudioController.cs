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

        //[HttpPost("generate")]
        //public async Task<IActionResult> GenerateImageAsync([FromForm] AiPromptDto req)
        //{
        //    if (req.ImageFile == null)
        //        return BadRequest(new { message = "Vui lòng chọn ảnh để tạo hình." });

        //    var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp", "image/gif", "image/bmp", "image/tiff", "image/svg+xml" };
        //    if (!allowedTypes.Contains(req.ImageFile.ContentType.ToLower()))
        //        return BadRequest(new { message = $"Định dạng ảnh không được hỗ trợ: {req.ImageFile.ContentType}" });

        //    const long maxFileSize = 50 * 1024 * 1024;
        //    if (req.ImageFile.Length > maxFileSize)
        //        return BadRequest(new { message = "Ảnh vượt quá dung lượng tối đa 50MB." });
        //    // --- (Kết thúc phần xác thực) ---

        //    try
        //    {
        //        using var ms = new MemoryStream();
        //        await req.ImageFile.CopyToAsync(ms);
        //        var base64 = Convert.ToBase64String(ms.ToArray());

        //        // ⭐️ TRUYỀN TẤT CẢ DỮ LIỆU SANG SERVICE
        //        var resultImage = await _aiService.GenerateImageAsync(
        //            base64,
        //            req.Prompt,
        //            req.Style,
        //            req.AspectRatio,
        //            req.Quality
        //        );

        //        return Ok(new { image = resultImage });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "Có lỗi xảy ra khi xử lý ảnh. Vui lòng thử lại sau.",
        //            detail = ex.Message
        //        });
        //    }
        //}

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateImageAsync([FromForm] AiPromptDto req)
        {
            // --- 1. VALIDATION (Giữ nguyên code cũ của bạn) ---
            if (req.ImageFile == null)
                return BadRequest(new { message = "Vui lòng chọn ảnh để tạo hình." });

            var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp" };
            if (!allowedTypes.Contains(req.ImageFile.ContentType.ToLower()))
                return BadRequest(new { message = $"Định dạng ảnh không được hỗ trợ: {req.ImageFile.ContentType}" });

            const long maxFileSize = 10 * 1024 * 1024; // Structure giới hạn khoảng 10MB, nên để 10-20MB là an toàn
            if (req.ImageFile.Length > maxFileSize)
                return BadRequest(new { message = "Ảnh vượt quá dung lượng tối đa (10MB)." });

            try
            {
                // --- 2. XỬ LÝ ẢNH INPUT ---
                using var ms = new MemoryStream();
                await req.ImageFile.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());

                // --- 3. XỬ LÝ LOGIC MAPPING ---

                // Map Frontend Quality -> Backend OutputFormat
                // Nếu Quality là "standard" -> dùng jpeg cho nhẹ.
                // Nếu Quality là "hd" hoặc null -> dùng png cho đẹp.
                string outputFormat = (req.Quality?.ToLower() == "standard") ? "jpeg" : "png";

                // Validate ControlStrength (chặn user gửi số âm hoặc > 1)
                double strength = Math.Clamp(req.ControlStrength, 0.0, 1.0);
                if (strength == 0) strength = 0.7; // Fallback nếu frontend quên gửi

                // --- 4. GỌI SERVICE ---
                // Lưu ý: Structure KHÔNG hỗ trợ thay đổi AspectRatio (req.AspectRatio),
                // nó luôn trả về ảnh cùng kích thước với ảnh gốc.
                var resultImage = await _aiService.GenerateStructureImageAsync(
                    base64Image: base64,
                    userPrompt: req.Prompt,
                    stylePreset: req.Style,       // Ví dụ: "photographic"
                    controlStrength: strength,    // Quan trọng: 0.7
                    seed: 0,                      // 0 = Random
                    outputFormat: outputFormat    // png/jpeg
                );

                return Ok(new { image = resultImage });
            }
            catch (Exception ex)
            {
                // Log lỗi ra console server để debug nếu cần
                Console.WriteLine($"Error Generating Image: {ex.Message}");

                return StatusCode(500, new
                {
                    message = "Có lỗi xảy ra khi xử lý ảnh.",
                    detail = ex.Message // Chỉ nên hiện detail trong môi trường Dev
                });
            }
        }
    }
}
