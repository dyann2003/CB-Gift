using CB_Gift.Services.IService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CB_Gift.Services
{
    public class AiStudioService : IAiStudioService
    {
        private readonly HttpClient _httpClient;
        //private readonly string _geminiApiKey;
        private readonly string _stabilityApiKey;
        private readonly ILogger<AiStudioService> _logger;
        public AiStudioService(IConfiguration config, ILogger<AiStudioService> logger)
        {
            _httpClient = new HttpClient();
            //_geminiApiKey = config["GeminiSettings:ApiKey"];
            _stabilityApiKey = config["StabilityAiSettings:ApiKey"];
            _logger = logger;
        }

        private const string staticInstructions = @"Based on the character in the source image, generate a 2D, completely flat, vector-style line art file suitable for a CNC router or laser cutter.
Critical Requirements (MUST follow):
Style: This must be a 2D technical drawing or blueprint, NOT a 3D render or artistic sketch.
No 3D/Shading: Absolutely NO 3D depth, NO shading, NO texturing, NO gradients, and NO grayscale. The output must be 100% flat.
Line Quality: Use clean, sharp, precise, single-weight black lines only.
Background: The background must be perfectly white and empty.
Simplification: Simplify the character's form. Retain only the essential outlines of the body, clothing, and major features (like eyes, nose) needed for a cut-out. All internal details must also be simple lines (no fill).
Final Output: The result should look like a CAD schematic or a vector file prepared for vinyl cutting.";

        public async Task<string> GenerateImageAsync(
            string base64Image,
            string userPrompt,
            string? style,
            string? aspectRatio,
            string? quality)
        {
            _logger.LogInformation("--- Bắt đầu gọi Stability AI (ControlNet / Sketch) ---");

            var stabilityUrl = "https://api.stability.ai/v2beta/stable-image/control/sketch";

            byte[] imageBytes;
            try
            {
                var pureBase64 = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;
                if (string.IsNullOrWhiteSpace(pureBase64))
                    throw new FormatException("Base64 string is empty or invalid after stripping prefix.");
                imageBytes = Convert.FromBase64String(pureBase64);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Lỗi giải mã Base64. Chuỗi đầu vào không hợp lệ.");
                throw new Exception("Invalid Base64 image string.", ex);
            }

            using var formData = new MultipartFormDataContent();

            // 1️. Ảnh đầu vào
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            imageContent.Headers.Add("Content-Disposition", "form-data; name=\"image\"; filename=\"image.png\"");
            formData.Add(imageContent);

            // 2️. Loại control
            var controlTypeContent = new StringContent("sketch", Encoding.UTF8);
            controlTypeContent.Headers.Add("Content-Disposition", "form-data; name=\"control_type\"");
            formData.Add(controlTypeContent);

            // 3️. Prompt (ưu tiên prompt người dùng nếu có)
            var finalPrompt = string.IsNullOrWhiteSpace(userPrompt)
                ? staticInstructions
                : $"{userPrompt}";
            var promptContent = new StringContent(finalPrompt, Encoding.UTF8);
            promptContent.Headers.Add("Content-Disposition", "form-data; name=\"prompt\"");
            formData.Add(promptContent);

            // 4️. Định dạng kết quả
            var outputFormatContent = new StringContent("png", Encoding.UTF8);
            outputFormatContent.Headers.Add("Content-Disposition", "form-data; name=\"output_format\"");
            formData.Add(outputFormatContent);
            if (!string.IsNullOrWhiteSpace(aspectRatio))
            {
                var aspectContent = new StringContent(aspectRatio, Encoding.UTF8);
                aspectContent.Headers.Add("Content-Disposition", "form-data; name=\"aspect_ratio\"");
                formData.Add(aspectContent);
            }

            if (!string.IsNullOrWhiteSpace(style))
            {
                var styleContent = new StringContent(style, Encoding.UTF8);
                styleContent.Headers.Add("Content-Disposition", "form-data; name=\"style\"");
                formData.Add(styleContent);
            }

            if (!string.IsNullOrWhiteSpace(quality))
            {
                var qualityContent = new StringContent(quality, Encoding.UTF8);
                qualityContent.Headers.Add("Content-Disposition", "form-data; name=\"quality\"");
                formData.Add(qualityContent);
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _stabilityApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var stabilityResponse = await _httpClient.PostAsync(stabilityUrl, formData);
            var responseBody = await stabilityResponse.Content.ReadAsStringAsync();

            if (!stabilityResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Stability AI API error: {ErrorContent}", responseBody);
                throw new Exception($"Stability AI error: {responseBody}");
            }

            _logger.LogInformation("--- Đã nhận ảnh từ Stability AI ---");
            var resultImageBytes = await stabilityResponse.Content.ReadAsByteArrayAsync();

            return "data:image/png;base64," + Convert.ToBase64String(resultImageBytes);
        }
    }
}