using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HuggingFaceController : ControllerBase
    {
        private readonly IHuggingFaceService _huggingFaceService;
        public HuggingFaceController(IHuggingFaceService huggingFaceService)
        {
            _huggingFaceService = huggingFaceService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] PromptDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Prompt))
                return BadRequest("Prompt không được rỗng");

            try
            {
                var imageUrl = await _huggingFaceService.GenerateImageAsync(dto.Prompt);
                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("generate-from-image")]
        public async Task<IActionResult> GenerateFromImage([FromForm] AiPromptDto dto)
        {
            if (dto.ImageFile == null || dto.ImageFile.Length == 0)
                return BadRequest("ImageFile không được rỗng");

            try
            {
                var imageUrl = await _huggingFaceService.GenerateImageFromImageAsync(dto.ImageFile, dto.Prompt);
                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
