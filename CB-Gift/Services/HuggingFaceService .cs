using CB_Gift.Services.IService;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CB_Gift.Services
{
    public class HuggingFaceService : IHuggingFaceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiToken;
        private readonly string _textModelUrl;
        private readonly string _imageModelUrl;

        public HuggingFaceService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _apiToken = config["HuggingFaceSettings:ApiToken"];
            _textModelUrl = config["HuggingFaceSettings:TextModelUrl"];
            _imageModelUrl = config["HuggingFaceSettings:ImageModelUrl"];
        }

        public async Task<string> GenerateImageAsync(string prompt)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiToken);

            var json = JsonSerializer.Serialize(new { inputs = prompt });
            var response = await client.PostAsync(
                _textModelUrl,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"HuggingFace error: {error}");
            }

            // --- đọc trực tiếp bytes image ---
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            string base64 = Convert.ToBase64String(imageBytes);
            return $"data:image/png;base64,{base64}";
        }

        public async Task<string> GenerateImageFromImageAsync(IFormFile imageFile, string prompt, float strength = 0.7f)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiToken);

            // Chuyển ảnh sang base64
            byte[] imageBytes;
            await using (var ms = new MemoryStream())
            {
                await imageFile.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }
            string base64Image = Convert.ToBase64String(imageBytes);

            // Tạo JSON payload
            var payload = new
            {
                init_image = base64Image,
                prompt = prompt,
                strength = strength
            };
            var json = JsonSerializer.Serialize(payload);

            var response = await client.PostAsync(
                _imageModelUrl,
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"HuggingFace error: {error}");
            }

            // Xử lý trả về
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(jsonResponse);
            string generatedImage = doc.RootElement[0].GetProperty("image").GetString();
            return $"data:image/png;base64,{generatedImage}";
        }
    }
}
