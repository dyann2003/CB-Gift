using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CB_Gift.Services.IService;

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

        [HttpPost("generate-chibi")]
        public async Task<IActionResult> GenerateChibi([FromForm] PromptImageDto req)
        {
            if (req.ImageFile == null)
                return BadRequest("Ảnh không được để trống.");

            using var ms = new MemoryStream();
            await req.ImageFile.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());

            var resultImage = await _aiService.GenerateLineArtFromChibiAsync(base64);
            return Ok(new { image = resultImage });
        }
    }

    public class PromptImageDto
    {
        public IFormFile ImageFile { get; set; }
    }
}
