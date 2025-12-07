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

        //        private const string staticInstructions = @"Based on the character in the source image, generate a 2D, completely flat, vector-style line art file suitable for a CNC router or laser cutter.
        //Critical Requirements (MUST follow):
        //Style: This must be a 2D technical drawing or blueprint, NOT a 3D render or artistic sketch.
        //No 3D/Shading: Absolutely NO 3D depth, NO shading, NO texturing, NO gradients, and NO grayscale. The output must be 100% flat.
        //Line Quality: Use clean, sharp, precise, single-weight black lines only.
        //Background: The background must be perfectly white and empty.
        //Simplification: Simplify the character's form. Retain only the essential outlines of the body, clothing, and major features (like eyes, nose) needed for a cut-out. All internal details must also be simple lines (no fill).
        //Final Output: The result should look like a CAD schematic or a vector file prepared for vinyl cutting.";

        //public async Task<string> GenerateImageAsync(
        //    string base64Image,
        //    string userPrompt,
        //    string? style,
        //    string? aspectRatio,
        //    string? quality)
        //{
        //    _logger.LogInformation("--- Bắt đầu gọi Stability AI (ControlNet / Sketch) ---");

        //    var stabilityUrl = "https://api.stability.ai/v2beta/stable-image/control/sketch";

        //    byte[] imageBytes;
        //    try
        //    {
        //        var pureBase64 = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;
        //        if (string.IsNullOrWhiteSpace(pureBase64))
        //            throw new FormatException("Base64 string is empty or invalid after stripping prefix.");
        //        imageBytes = Convert.FromBase64String(pureBase64);
        //    }
        //    catch (FormatException ex)
        //    {
        //        _logger.LogError(ex, "Lỗi giải mã Base64. Chuỗi đầu vào không hợp lệ.");
        //        throw new Exception("Invalid Base64 image string.", ex);
        //    }

        //    using var formData = new MultipartFormDataContent();

        //    // 1️. Ảnh đầu vào
        //    var imageContent = new ByteArrayContent(imageBytes);
        //    imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        //    imageContent.Headers.Add("Content-Disposition", "form-data; name=\"image\"; filename=\"image.png\"");
        //    formData.Add(imageContent);

        //    // 2️. Loại control
        //    var controlTypeContent = new StringContent("sketch", Encoding.UTF8);
        //    controlTypeContent.Headers.Add("Content-Disposition", "form-data; name=\"control_type\"");
        //    formData.Add(controlTypeContent);

        //    // 3️. Prompt (ưu tiên prompt người dùng nếu có)
        //    var finalPrompt = string.IsNullOrWhiteSpace(userPrompt)
        //        ? staticInstructions
        //        : $"{userPrompt}";
        //    var promptContent = new StringContent(finalPrompt, Encoding.UTF8);
        //    promptContent.Headers.Add("Content-Disposition", "form-data; name=\"prompt\"");
        //    formData.Add(promptContent);

        //    // 4️. Định dạng kết quả
        //    var outputFormatContent = new StringContent("png", Encoding.UTF8);
        //    outputFormatContent.Headers.Add("Content-Disposition", "form-data; name=\"output_format\"");
        //    formData.Add(outputFormatContent);
        //    if (!string.IsNullOrWhiteSpace(aspectRatio))
        //    {
        //        var aspectContent = new StringContent(aspectRatio, Encoding.UTF8);
        //        aspectContent.Headers.Add("Content-Disposition", "form-data; name=\"aspect_ratio\"");
        //        formData.Add(aspectContent);
        //    }

        //    if (!string.IsNullOrWhiteSpace(style))
        //    {
        //        var styleContent = new StringContent(style, Encoding.UTF8);
        //        styleContent.Headers.Add("Content-Disposition", "form-data; name=\"style\"");
        //        formData.Add(styleContent);
        //    }

        //    if (!string.IsNullOrWhiteSpace(quality))
        //    {
        //        var qualityContent = new StringContent(quality, Encoding.UTF8);
        //        qualityContent.Headers.Add("Content-Disposition", "form-data; name=\"quality\"");
        //        formData.Add(qualityContent);
        //    }

        //    _httpClient.DefaultRequestHeaders.Clear();
        //    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _stabilityApiKey);
        //    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
        //    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        //    var stabilityResponse = await _httpClient.PostAsync(stabilityUrl, formData);
        //    var responseBody = await stabilityResponse.Content.ReadAsStringAsync();

        //    if (!stabilityResponse.IsSuccessStatusCode)
        //    {
        //        _logger.LogError("Stability AI API error: {ErrorContent}", responseBody);
        //        throw new Exception($"Stability AI error: {responseBody}");
        //    }

        //    _logger.LogInformation("--- Đã nhận ảnh từ Stability AI ---");
        //    var resultImageBytes = await stabilityResponse.Content.ReadAsByteArrayAsync();

        //    return "data:image/png;base64," + Convert.ToBase64String(resultImageBytes);
        //}

        // --- 2. Hàm cho STRUCTURE ---
        public async Task<string> GenerateStructureImageAsync(
            string base64Image,
            string userPrompt,
            string? stylePreset = "photographic",
            double controlStrength = 0.7,
            long? seed = 0,
            string outputFormat = "png")
        {
            _logger.LogInformation("--- Bắt đầu gọi Stability AI (Control: Structure) ---");

            var structureUrl = "https://api.stability.ai/v2beta/stable-image/control/structure";

            // 1. Xử lý ảnh Input
            byte[] imageBytes;
            try
            {
                var pureBase64 = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;
                if (string.IsNullOrWhiteSpace(pureBase64)) throw new FormatException("Base64 string is empty.");
                imageBytes = Convert.FromBase64String(pureBase64);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Lỗi giải mã Base64.");
                throw new Exception("Invalid Base64 image string.", ex);
            }

            using var formData = new MultipartFormDataContent();

            // [image]
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            imageContent.Headers.Add("Content-Disposition", "form-data; name=\"image\"; filename=\"input.png\"");
            formData.Add(imageContent);

            // [prompt]
            var promptContent = new StringContent(userPrompt, Encoding.UTF8);
            promptContent.Headers.Add("Content-Disposition", "form-data; name=\"prompt\"");
            formData.Add(promptContent);

            // [control_strength] - Quan trọng
            var strengthStr = controlStrength.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            var strengthContent = new StringContent(strengthStr, Encoding.UTF8);
            strengthContent.Headers.Add("Content-Disposition", "form-data; name=\"control_strength\"");
            formData.Add(strengthContent);

            // [style_preset]
            if (!string.IsNullOrWhiteSpace(stylePreset))
            {
                var styleContent = new StringContent(stylePreset, Encoding.UTF8);
                styleContent.Headers.Add("Content-Disposition", "form-data; name=\"style_preset\"");
                formData.Add(styleContent);
            }

            // [seed] - MỚI: Nếu seed > 0 thì gửi lên, giúp tái tạo kết quả cũ
            if (seed.HasValue && seed.Value > 0)
            {
                var seedContent = new StringContent(seed.Value.ToString(), Encoding.UTF8);
                seedContent.Headers.Add("Content-Disposition", "form-data; name=\"seed\"");
                formData.Add(seedContent);
            }

            // [output_format] - MỚI: Xử lý png/jpeg/webp
            // Validate để đảm bảo chỉ gửi giá trị hợp lệ
            var validFormats = new[] { "png", "jpeg", "webp" };
            var finalFormat = validFormats.Contains(outputFormat.ToLower()) ? outputFormat.ToLower() : "png";

            var formatContent = new StringContent(finalFormat, Encoding.UTF8);
            formatContent.Headers.Add("Content-Disposition", "form-data; name=\"output_format\"");
            formData.Add(formatContent);

            // [negative_prompt] - Mặc định để ảnh sạch
            string defaultNegative = "ugly, deformed, disfigured, poor details, bad anatomy, text, watermark, blurry, low quality";
            var negativeContent = new StringContent(defaultNegative, Encoding.UTF8);
            negativeContent.Headers.Add("Content-Disposition", "form-data; name=\"negative_prompt\"");
            formData.Add(negativeContent);

            // Gửi Request
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _stabilityApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

            var response = await _httpClient.PostAsync(structureUrl, formData);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Stability AI Error ({Code}): {Msg}", response.StatusCode, errorBody);

                // Thử parse JSON lỗi để lấy message gọn hơn nếu cần
                throw new Exception($"Stability AI Error: {errorBody}");
            }

            _logger.LogInformation("--- Đã nhận ảnh Structure thành công ---");
            var resultImageBytes = await response.Content.ReadAsByteArrayAsync();

            // Trả về đúng định dạng mime type dựa trên output format
            string mimeType = finalFormat == "jpeg" ? "image/jpeg" : "image/png";
            return $"data:{mimeType};base64," + Convert.ToBase64String(resultImageBytes);
        }
    }
}