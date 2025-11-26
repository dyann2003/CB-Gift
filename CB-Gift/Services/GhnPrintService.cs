using System.Text;
using System.Text.Json;
using CB_Gift.Services.IService;
using CB_Gift.DTOs;

namespace CB_Gift.Services
{
    public class GhnPrintService : IGhnPrintService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<GhnPrintService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public GhnPrintService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GhnPrintService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = configuration;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        //Lấy URL in
        public async Task<string> GetPrintUrlAsync(List<string> orderCodes, string size = "A5")
        {
            if (orderCodes == null || !orderCodes.Any())
            {
                throw new ArgumentException("Danh sách mã vận đơn không được rỗng.");
            }

            // 1. Lấy token (truyền cả danh sách)
            var token = await GetPrintTokenAsync(orderCodes);
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Không thể lấy token in từ GHN.");
            }

            // 2. Chọn URL HTML
            var printUrlBase = size switch
            {
                "A5" => _config["GhnSettings:PrintA5Url"],
                "80x80" => _config["GhnSettings:Print80x80Url"],
                "52x70" => _config["GhnSettings:Print52x70Url"],
                _ => throw new ArgumentException("Kích thước vận đơn không hợp lệ.")
            };

            // 3. Trả về link hoàn chỉnh
            var finalUrl = $"{printUrlBase}?token={token}";
            _logger.LogInformation($"Tạo link in GHN: {finalUrl}");
            return finalUrl;
        }

        // Lấy URL xong tải luôn HTML về (Fix CORS)
        public async Task<string> GetPrintHtmlContentAsync(List<string> orderCodes, string size = "A5")
        {
            // Bước 1: Lấy Link
            string url = await GetPrintUrlAsync(orderCodes, size);

            try
            {
                // Bước 2: Dùng HttpClient tải nội dung HTML về server
                // Sử dụng Client thường vì URL này là public, không cần header Auth của GHN API
                var client = _httpClientFactory.CreateClient();

                _logger.LogInformation($"Backend đang tải HTML từ: {url}");
                string htmlContent = await client.GetStringAsync(url);

                return htmlContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải nội dung HTML từ GHN");
                throw new Exception("Không thể tải nội dung phiếu in từ GHN. Vui lòng thử lại.");
            }
        }

        // Hàm private: Lấy token
        private async Task<string> GetPrintTokenAsync(List<string> orderCodes)
        {
            var client = _httpClientFactory.CreateClient("GhnDevClient");
            var genTokenUrl = _config["GhnSettings:PrintTokenUrl"];

            var payload = new { order_codes = orderCodes };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(genTokenUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Lỗi khi gen-token GHN: {responseBody}");
                    throw new InvalidOperationException("Lỗi khi lấy token in từ GHN.");
                }

                var tokenResponse = JsonSerializer.Deserialize<GhnBaseResponse<GhnPrintTokenData>>(responseBody, _jsonOptions);

                if (tokenResponse?.Code != 200 || string.IsNullOrEmpty(tokenResponse.Data?.Token))
                {
                    _logger.LogError($"Lỗi logic khi gen-token: {responseBody}");
                    throw new InvalidOperationException(tokenResponse?.Message ?? "Không nhận được token.");
                }

                return tokenResponse.Data.Token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng khi gọi GetPrintTokenAsync");
                throw;
            }
        }
    }
}