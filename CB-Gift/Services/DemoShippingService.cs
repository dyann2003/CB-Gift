using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace CB_Gift.Services
{
    public class DemoShippingService : IShippingService
    {
        private readonly GhnShippingService _realService;
        private readonly CBGiftDbContext _context; 
        private readonly ILogger<DemoShippingService> _logger;

        // Dictionary lưu mốc thời gian bắt đầu track
        private static readonly ConcurrentDictionary<string, DateTime> _trackingStartTimes
            = new ConcurrentDictionary<string, DateTime>();

        public DemoShippingService(
            GhnShippingService realService,
            CBGiftDbContext context,
            ILogger<DemoShippingService> logger)
        {
            _realService = realService;
            _context = context;
            _logger = logger;
        }

        // 1. TẠO ĐƠN: Vẫn gọi GHN thật, nhưng sau đó lưu Log khởi tạo vào DB
        public async Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request)
        {
            // Gọi GHN thật để lấy mã vận đơn chuẩn (để in được phiếu)
            var result = await _realService.CreateOrderAsync(request);

            // Ghi log đầu tiên vào DB: "ready_to_pick"
            var initialLog = new GhnTrackingLog
            {
                OrderCode = result.OrderCode,
                Status = "ready_to_pick",
                UpdatedDate = DateTime.Now
            };

            _context.GhnTrackingLogs.Add(initialLog);
            await _context.SaveChangesAsync();

            return result;
        }

        // 2. TRACKING: Lấy Info từ GHN thật + Override Status/Log từ DB
        public async Task<TrackingResult> TrackOrderAsync(string orderCode)
        {
            // 1. Gọi API GHN thật
            // Mục đích: Lấy ToName, ToPhone, ToAddress, Items, Weight... chính xác 100%
            var result = await _realService.TrackOrderAsync(orderCode);

            // 2. Lấy danh sách Log từ Database của bạn
            var dbLogs = await _context.GhnTrackingLogs
                .Where(x => x.OrderCode == orderCode)
                .OrderBy(x => x.UpdatedDate) // Sắp xếp từ cũ đến mới
                .ToListAsync();

            // 3. Ghi đè Status và Log (Nếu trong DB có dữ liệu)
            if (dbLogs.Any())
            {
                // Lấy log mới nhất làm Status hiện tại
                var latestLog = dbLogs.Last();
                result.Status = latestLog.Status;

                // Nếu đã giao thành công (delivered), cập nhật ngày giao hàng (Leadtime)
                if (latestLog.Status == "delivered")
                {
                    result.Leadtime = latestLog.UpdatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                }

                // Xóa log của GHN đi, thay bằng log từ DB
                result.Log = dbLogs.Select(l => new GhnLog
                {
                    Status = l.Status,
                    UpdatedDate = l.UpdatedDate.ToString("yyyy-MM-dd HH:mm:ss") // Format đúng yêu cầu
                }).ToList();
            }
            else
            {
                if (result.Log == null)
                {
                    result.Log = new List<GhnLog>();
                }
                if (result.Leadtime != null && DateTime.TryParse(result.Leadtime.ToString(), out DateTime leadTimeDate))
                {
                    result.Leadtime = leadTimeDate.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }

            return result;
        }

        // 3. HÀM CẬP NHẬT TRẠNG THÁI (Dùng cho UI Admin set bằng tay)
    /*    public async Task ManualUpdateStatusAsync(string trackingCode, string newStatus)
        {
            // === BƯỚC 1: Debug Input ===
            Console.WriteLine($"[DEBUG] Bắt đầu Update: Code='{trackingCode}', NewStatus='{newStatus}'");

            var code = trackingCode.Trim();
            var status = newStatus.Trim().ToLower();

            // === BƯỚC 2: Thêm Log GhnTracking ===
            var log = new GhnTrackingLog
            {
                OrderCode = code,
                Status = status,
                UpdatedDate = DateTime.Now
            };
            _context.GhnTrackingLogs.Add(log);

            // === BƯỚC 3: Lấy Order ===
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Tracking == code);

            if (order == null)
            {
                throw new Exception($"[LỖI] Không tìm thấy đơn hàng với mã: {code}");
            }

            Console.WriteLine($"[DEBUG] Tìm thấy Order ID: {order.OrderId} | StatusOrder cũ: {order.StatusOrder}");

            // === BƯỚC 4: Switch Case & Cập nhật ===
            bool isChanged = false;

            switch (status)
            {
                case "ready_to_pick": // QC Done
                    order.StatusOrder = 11;
                    isChanged = true;
                    break;

                case "shipping":
                case "picking": // Shipping
                    order.StatusOrder = 13;
                    isChanged = true;
                    break;

                case "shipped":
                case "delivered": // Shipped
                    order.StatusOrder = 14;
                    isChanged = true;
                    break;

                default:
                    Console.WriteLine($"[WARNING] Status '{status}' không khớp với case nào!");
                    break;
            }

            // === BƯỚC 5: FORCE UPDATE (Quan trọng) ===
            if (isChanged)
            {
                // Dòng này ép buộc EF Core đánh dấu object này đã bị sửa -> Bắt buộc phải lưu xuống DB
                _context.Entry(order).State = EntityState.Modified;

                Console.WriteLine($"[DEBUG] Đã đổi StatusOrder sang: {order.StatusOrder}. Chuẩn bị Save.");
            }

            // === BƯỚC 6: Lưu DB ===
            var result = await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] SaveChanges thành công. Số dòng ảnh hưởng: {result}");
        }*/
        public async Task ManualUpdateStatusAsync(string trackingCode, string newStatus)
        {
            // === BƯỚC 1: Debug Input ===
            Console.WriteLine($"[DEBUG] Bắt đầu Update: Code='{trackingCode}', NewStatus='{newStatus}'");

            var code = trackingCode.Trim();
            var status = newStatus.Trim().ToLower();

            // === BƯỚC 2: Thêm Log GhnTracking (Giữ nguyên) ===
            var log = new GhnTrackingLog
            {
                OrderCode = code,
                Status = status,
                UpdatedDate = DateTime.Now
            };
            _context.GhnTrackingLogs.Add(log);

            // === BƯỚC 3: Lấy Order KÈM THEO OrderDetails (QUAN TRỌNG) ===
            var order = await _context.Orders
                                      .Include(o => o.OrderDetails) // <--- Phải có dòng này mới sửa được bảng con
                                      .FirstOrDefaultAsync(o => o.Tracking == code);

            if (order == null)
            {
                throw new Exception($"[LỖI] Không tìm thấy đơn hàng với mã: {code}");
            }

            Console.WriteLine($"[DEBUG] Tìm thấy Order ID: {order.OrderId} | StatusOrder cũ: {order.StatusOrder}");

            // === BƯỚC 4: Switch Case & Cập nhật ===
            bool isChanged = false;

            switch (status)
            {
                case "ready_to_pick": // QC Done / Chờ lấy hàng
                    order.StatusOrder = 11;
                    isChanged = true;
                    break;

                case "shipping":
                case "picking": // Đang giao hàng
                    order.StatusOrder = 13;

                    // (Optional) Nên cập nhật cả Shipping ở đây cho đồng bộ
                  //  order.ProductionStatus = "SHIPPING";
                    if (order.OrderDetails != null)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            detail.ProductionStatus = ProductionStatus.SHIPPING;
                        }
                    }
                    isChanged = true;
                    break;

                case "shipped":
                case "delivered": // Đã giao hàng thành công
                                  // 1. Cập nhật trạng thái chung của Order
                    order.StatusOrder = 14;
                   // order.ProductionStatus = "SHIPPED"; // Cập nhật string hiển thị của Order cha

                    // 2. Cập nhật trạng thái từng sản phẩm con (OrderDetail)
                    if (order.OrderDetails != null)
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            // Gán Enum SHIPPED (Value = 13 trong Enum của bạn)
                            detail.ProductionStatus = ProductionStatus.SHIPPED;
                        }
                    }

                    isChanged = true;
                    break;

                default:
                    Console.WriteLine($"[WARNING] Status '{status}' không khớp với case nào!");
                    break;
            }

            // === BƯỚC 5: FORCE UPDATE ===
            if (isChanged)
            {
                _context.Entry(order).State = EntityState.Modified;
                Console.WriteLine($"[DEBUG] Đã đổi StatusOrder sang: {order.StatusOrder}. Chuẩn bị Save.");
            }

            // === BƯỚC 6: Lưu DB ===
            var result = await _context.SaveChangesAsync();

            // (Optional) Gửi SignalR cập nhật realtime cho FE ở đây nếu cần

            Console.WriteLine($"[DEBUG] SaveChanges thành công. Số dòng ảnh hưởng: {result}");
        }
    }
}