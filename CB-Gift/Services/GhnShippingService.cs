using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CB_Gift.Services
{
    public class GhnShippingService : IShippingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GhnShippingService> _logger;
        private readonly CBGiftDbContext _context;
        private readonly IConfiguration _config;
        private readonly int _shopId;

        private readonly JsonSerializerOptions _jsonOptions;

        public GhnShippingService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GhnShippingService> logger, CBGiftDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = configuration;
            _context = context;
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
            //LOGIC ĐỒNG BỘ STATUS VÀO DB (MỚI THÊM VÀO)
            //await SyncGhnStatusToLocalDbAsync(orderCode, data.Status);

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
        private async Task SyncGhnStatusToLocalDbAsync(string trackingCode, string ghnStatus)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Tracking == trackingCode);
                if (order == null) return; // Không tìm thấy thì thôi, không lỗi

                bool isChanged = false;
                string status = ghnStatus.ToLower();

                // Logic Mapping Status (Giống hệt ManualGhnService)
                // GHN có rất nhiều status con (picking, storing, sorting...), ta gom hết về Shipping

                // 1. Ready To Pick
                if (status == "ready_to_pick" && order.StatusOrder != 11)
                {
                    order.StatusOrder = 11; // QC_Done
                    isChanged = true;
                }
                // 2. Shipping (Gồm picking, transporting, sorting, storing...)
                else if ((status == "picking" || status == "transporting" || status == "sorting" || status == "storing" || status == "shipping")
                         && order.StatusOrder != 13)
                {
                    order.StatusOrder = 13; // Shipping
                    isChanged = true;
                }
                // 3. Shipped (delivered)
                else if ((status == "delivered" || status == "shipped") && order.StatusOrder != 14)
                {
                    order.StatusOrder = 14; // Shipped
                    isChanged = true;
                }
                //// 4. Return (cancel, return)
                //else if ((status == "cancel" || status == "return" || status == "returned") && order.StatusOrder != 15)
                //{
                //    order.StatusOrder = 15; // Cancelled/Returned
                //    isChanged = true;
                //}

                if (isChanged)
                {
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"[SYNC] Đã cập nhật đơn {trackingCode} sang StatusOrder: {order.StatusOrder} (GHN: {ghnStatus})");
                }
            }
            catch (Exception ex)
            {
                // Chỉ log lỗi, không throw exception để tránh làm hỏng luồng hiển thị Tracking của khách
                _logger.LogError(ex, $"Lỗi khi đồng bộ status đơn {trackingCode} vào DB.");
            }
        }

    }
}