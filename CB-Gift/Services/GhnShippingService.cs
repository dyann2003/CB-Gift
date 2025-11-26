using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using System.Text;
using System.Text.Json;

namespace CB_Gift.Services
{
    public class GhnShippingService : IShippingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GhnShippingService> _logger;
        private readonly IConfiguration _config;
        private readonly int _shopId;

        private readonly JsonSerializerOptions _jsonOptions;

        public GhnShippingService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GhnShippingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = configuration;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            _shopId = _config.GetValue<int>("GhnSettings:ShopId");
            if (_shopId == 0)
                throw new InvalidOperationException("GhnSettings:ShopId chưa được cấu hình.");
        }

        public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request)
        {
            var client = _httpClientFactory.CreateClient("GhnDevClient");

            // URL TƯƠNG ĐỐI
            var createOrderUrl = _config["GhnSettings:CreateOrderUrl"];

            var ghnPayload = new
            {

                // ======== FROM =========
                from_name = "CB-Gift Shop",
                from_phone = "0900123456",
                from_address = "Số 1 Lê Đức Thọ",
                from_ward_name = "Phường Mai Dịch",
                from_district_name = "Quận Cầu Giấy",
                from_province_name = "Hà Nội",

                // ======== TO =========
                to_name = request.ToName,
                to_phone = request.ToPhone,
                to_address = request.ToAddress,
                to_ward_code = request.ToWardCode,
                to_district_id = request.ToDistrictId,

                // ======== THUỘC TÍNH KIỆN HÀNG =========
                weight = request.WeightInGrams,
                length = request.Length,
                width = request.Width,
                height = request.Height,

                // ======== DỊCH VỤ =========
                service_id = 53320,
                service_type_id = 2,

                payment_type_id = 1,
                required_note = "CHOXEMHANGKHONGTHU",

                // ======== ITEMS =========
                items = request.Items.Select(i => new {
                    name = i.Name,
                    quantity = i.Quantity,
                    weight = i.Weight
                }).ToList()

            };

            var jsonPayload = JsonSerializer.Serialize(ghnPayload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Gọi URL TƯƠNG ĐỐI
            var response = await client.PostAsync(createOrderUrl, content);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"GHN CreateOrder response: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"GHN API lỗi: {responseBody}");
            }

            var ghnResponse = JsonSerializer.Deserialize<GhnBaseResponse<GhnCreateOrderData>>(responseBody, _jsonOptions);
            if (ghnResponse?.Code != 200)
                throw new InvalidOperationException(ghnResponse?.Message ?? "Không thể tạo đơn hàng.");

            return new CreateOrderResult
            {
                OrderCode = ghnResponse.Data.OrderCode,
                TotalFee = ghnResponse.Data.TotalFee
            };
        }


        public async Task<TrackingResult> TrackOrderAsync(string orderCode)
        {
            var client = _httpClientFactory.CreateClient("GhnDevClient");

             var trackOrderUrl = _config["GhnSettings:TrackOrderUrl"];
            var payload = new { order_code = orderCode };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(trackOrderUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("GHN Response: {response}", responseBody);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"GHN TrackOrder lỗi: {responseBody}");
                throw new InvalidOperationException($"Lỗi từ GHN khi theo dõi đơn: {responseBody}");
            }
            var ghnResponse = JsonSerializer.Deserialize<GhnBaseResponse<TrackingResult>>(responseBody, _jsonOptions);
            Console.Write(ghnResponse);

            if (ghnResponse == null || ghnResponse.Code != 200)
                throw new InvalidOperationException(ghnResponse?.Message ?? "Không tìm thấy đơn.");

            var data = ghnResponse.Data;

            if (data == null)
            {
                throw new InvalidOperationException("Không tìm thấy dữ liệu đơn hàng.");
            }

            return new TrackingResult
            {
                OrderCode = data.OrderCode,
                Status = data.Status,
                OrderDate = data.OrderDate,
                Leadtime = data.Leadtime,
                PickupTime = data.PickupTime,
                ToName = data.ToName,
                ToPhone = data.ToPhone,
                ToAddress = data.ToAddress,
                RequiredNote = data.RequiredNote,
                Weight = data.Weight,
                Items  = data.Items ?? new List<GhnItem>(),
                Log = data.Log ?? new List<GhnLog>(),
            };
        }

    }
}