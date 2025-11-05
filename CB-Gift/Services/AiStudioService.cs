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

        public async Task<string> GenerateLineArtFromChibiAsync(string base64Image, string userPrompt)
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





        //        private const string staticInstructions = @"
        //Based on the character in this image, convert it into a clean black-and-white line art specifically designed for CNC or laser cutting.
        //Keep only the outlines and essential details; remove all colors, shading, textures, and backgrounds.
        //Ensure lines are continuous, smooth, precise, and clearly defined, maintaining the original proportions and pose of the character.
        //The final output should be a high-contrast vector-style illustration suitable for cutting, with no background or extra details.";

        //        public async Task<string> GenerateLineArtFromChibiAsync(string base64Image)
        //        {
        //            _logger.LogInformation("--- Bước 1: Bắt đầu gọi Gemini API ---");
        //            string geminiDescription = "";
        //            try
        //            {
        //                var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";
        //                var geminiBody = new
        //                {
        //                    contents = new[]
        //                    {
        //                        new
        //                        {
        //                            parts = new object[]
        //                            {
        //                                new { inline_data = new { mime_type = "image/png", data = base64Image } },
        //                                new { text = "Describe this character for an image generation prompt." }    
        //                            }
        //                        }
        //                    }
        //                };

        //                _httpClient.DefaultRequestHeaders.Clear();
        //                var geminiResponse = await _httpClient.PostAsync(
        //                    geminiUrl,
        //                    new StringContent(JsonSerializer.Serialize(geminiBody), Encoding.UTF8, "application/json")
        //                );
        //                var geminiContent = await geminiResponse.Content.ReadAsStringAsync();

        //                if (!geminiResponse.IsSuccessStatusCode)
        //                {
        //                    _logger.LogError("Gemini API error: {ErrorContent}", geminiContent);
        //                    throw new Exception($"Gemini API error: {geminiContent}");
        //                }

        //                using var geminiDoc = JsonDocument.Parse(geminiContent);
        //                if (geminiDoc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0 &&
        //                    candidates[0].TryGetProperty("content", out var content) &&
        //                    content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0 &&
        //                    parts[0].TryGetProperty("text", out var textProp))
        //                {
        //                    geminiDescription = textProp.GetString() ?? "";
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                _logger.LogError(ex, "Lỗi trong quá trình gọi hoặc parse Gemini API.");
        //                geminiDescription = "A chibi character."; // Fallback
        //            }

        //            string finalPrompt = $"{geminiDescription}\n\n{staticInstructions}";
        //            _logger.LogInformation("--- Bước 2: Bắt đầu gọi Stability AI ---");
        //            _logger.LogInformation("Final Prompt Sent to Stability AI: {FinalPrompt}", finalPrompt);

        //            var stabilityUrl = "https://api.stability.ai/v2beta/stable-image/generate/sd3";

        //            using var formData = new MultipartFormDataContent();

        //            var promptContent = new StringContent(finalPrompt, Encoding.UTF8);
        //            promptContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        //            {
        //                Name = "\"prompt\""
        //            };
        //            promptContent.Headers.ContentType = null;
        //            formData.Add(promptContent);

        //            var formatContent = new StringContent("png", Encoding.UTF8);
        //            formatContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        //            {
        //                Name = "\"output_format\""
        //            };
        //            formatContent.Headers.ContentType = null;
        //            formData.Add(formatContent);

        //            var noneContent = new StringContent("", Encoding.UTF8, "text/plain");
        //            noneContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
        //            {
        //                Name = "\"none\"",
        //                FileName = "\"\""
        //            };
        //            formData.Add(noneContent);

        //            _httpClient.DefaultRequestHeaders.Clear();
        //            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _stabilityApiKey);
        //            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

        //            var stabilityResponse = await _httpClient.PostAsync(stabilityUrl, formData);

        //            if (!stabilityResponse.IsSuccessStatusCode)
        //            {
        //                var err = await stabilityResponse.Content.ReadAsStringAsync();
        //                _logger.LogError("Stability AI API error: {ErrorContent}", err);
        //                throw new Exception($"Stability AI error: {err}");
        //            }

        //            _logger.LogInformation("--- Bước 3: Đã nhận ảnh từ Stability AI ---");
        //            var imageBytes = await stabilityResponse.Content.ReadAsByteArrayAsync();

        //            return Convert.ToBase64String(imageBytes);
        //        }
    }
}