using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using System.Text.Json;

namespace CB_Gift.Services
{
    public class GhnLocationService : ILocationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GhnLocationService> _logger;
        private readonly IConfiguration _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public GhnLocationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GhnLocationService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = configuration;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<IEnumerable<GhnProvince>> GetProvincesAsync()
        {
            // Lấy URL tương đối
            var relativeUrl = _config["GhnSettings:GetProvincesUrl"];
            return await CallGhnApiAsync<IEnumerable<GhnProvince>>(relativeUrl, "lấy Tỉnh/Thành");
        }

        public async Task<IEnumerable<GhnDistrict>> GetDistrictsAsync(int provinceId)
        {
            var baseUrl = _config["GhnSettings:GetDistrictsUrl"];
            // Nối URL tương đối với tham số
            var relativeUrl = $"{baseUrl}?province_id={provinceId}";
            return await CallGhnApiAsync<IEnumerable<GhnDistrict>>(relativeUrl, "lấy Quận/Huyện");
        }

        public async Task<IEnumerable<GhnWard>> GetWardsAsync(int districtId)
        {
            var baseUrl = _config["GhnSettings:GetWardsUrl"];
            var relativeUrl = $"{baseUrl}?district_id={districtId}";
            return await CallGhnApiAsync<IEnumerable<GhnWard>>(relativeUrl, "lấy Phường/Xã");
        }

        // Hàm helper này giờ gọi URL TƯƠNG ĐỐI
        private async Task<T> CallGhnApiAsync<T>(string relativeUrl, string logContext)
        {
            var client = _httpClientFactory.CreateClient("GhnProdClient");

            try
            {
                // Client đã có BaseAddress, nên chỉ cần truyền URL tương đối
                var response = await client.GetAsync(relativeUrl);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // URL ở đây sẽ hiển thị là URL tương đối, 
                    // nhưng bạn có thể log thêm client.BaseAddress nếu muốn
                    _logger.LogWarning($"GHN API lỗi khi {logContext}: {responseBody} | URL: {relativeUrl}");
                    throw new InvalidOperationException($"Lỗi từ GHN khi {logContext}");
                }

                var ghnResponse = JsonSerializer.Deserialize<GhnBaseResponse<T>>(responseBody, _jsonOptions);

                if (ghnResponse?.Code != 200)
                {
                    throw new InvalidOperationException(ghnResponse?.Message ?? $"Lỗi không xác định khi {logContext}");
                }
                return ghnResponse.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi nghiêm trọng khi {logContext}");
                // Ném lỗi gốc để hiểu rõ hơn
                throw;
            }
        }
    }
}