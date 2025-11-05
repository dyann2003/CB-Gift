using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CB_Gift.Hubs;
using Microsoft.Extensions.Logging;

namespace CB_Gift.Services
{
    public class CancellationService : ICancellationService
    {
        private readonly CBGiftDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<CancellationService> _logger;

        public CancellationService(CBGiftDbContext context, INotificationService notificationService,
            IHubContext<NotificationHub> hubContext,
            ILogger<CancellationService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task RequestCancellationAsync(int orderId, CancelRequestDto request, string sellerId)
        {
            var order = await _context.Orders
                .Include(o => o.StatusOrderNavigation)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerUserId == sellerId);

            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.");
            if (order.StatusOrderNavigation == null)
                throw new InvalidOperationException($"Không thể tải thông tin trạng thái cho OrderID: {orderId}.");

            // === KIỂM TRA NGHIỆP VỤ MỚI ===

            // 1. Các trạng thái được phép yêu cầu hủy
            // 7 = CONFIRMED (Chưa sản xuất)
            // 9-15 = Các trạng thái ĐÃ VÀO SẢN XUẤT (INPROD, FINISHED, QC_DONE, QC_FAIL, PACKING, PROD_REWORK)
            var allowedStatuses = new[] { 7,8, 9, 10, 11, 12, 13, 15 };

            if (!allowedStatuses.Contains(order.StatusOrder))
            {
                throw new InvalidOperationException($"Đơn hàng ở trạng thái '{order.StatusOrderNavigation.NameVi}' không thể yêu cầu hủy.");
            }

            // 2. Kiểm tra các điều kiện an toàn khác (Hóa đơn, Thanh toán)
            if (order.PaymentStatus == "Paid") // Chỉ cho phép hủy đơn "Unpaid" (Logic cũ)
                throw new InvalidOperationException("Đơn hàng đã thanh toán không thể yêu cầu hủy (phải dùng luồng Refund).");

            bool onActiveInvoice = await _context.InvoiceItems.AnyAsync(
                ii => ii.OrderId == orderId && ii.Invoice.Status != "Canceled");
            if (onActiveInvoice)
                throw new InvalidOperationException("Đơn hàng đã được đưa vào hóa đơn đang hoạt động, không thể hủy.");

            bool alreadyPending = await _context.CancellationRequests.AnyAsync(
                r => r.OrderId == orderId && r.Status == "Pending");
            if (alreadyPending)
                throw new InvalidOperationException("Đơn hàng này đã có yêu cầu hủy đang chờ xử lý.");

            // === THỰC THI LOGIC ===

            // TẠO YÊU CẦU HỦY MỚI
            var newRequest = new CancellationRequest
            {
                OrderId = orderId,
                RequestedByUserId = sellerId,
                RequestReason = request.Reason,
                Status = "Pending",
                PreviousStatusOrder = order.StatusOrderNavigation.Code
            };
            _context.CancellationRequests.Add(newRequest);

            // CẬP NHẬT TRẠNG THÁI ORDER
            order.StatusOrder = 16; // 16 = HOLD (Chuyển sang "Chờ")

            // CẬP NHẬT PRODUCTION_STATUS THEO YÊU CẦU
            var inProductionStatuses = new[] { 9, 10, 11, 12, 13, 15 };
            if (inProductionStatuses.Contains(order.StatusOrder))
            {
                order.ProductionStatus = "TinhBaseCost";
            }

            await _context.SaveChangesAsync();
            // ✅ BẮT ĐẦU GỬI THÔNG BÁO (SAU KHI LƯU)
            try
            {
                // 1. Gửi cập nhật trạng thái real-time cho bất kỳ ai đang xem đơn hàng
                var orderGroupName = $"order_{orderId}";
                await _hubContext.Clients.Group(orderGroupName).SendAsync(
                    "OrderStatusChanged",
                    new { orderId = orderId, newStatus = "HOLD", newStatusId = 16 }
                );

                // 2. Gửi sự kiện "có request mới" cho các Staff dashboards
                // (Giả sử Staff frontend sẽ join group "StaffNotifications")
                await _hubContext.Clients.Group("StaffNotifications").SendAsync(
                    "NewCancelRequest",
                    new { orderId = orderId, sellerId = sellerId, reason = request.Reason, newRequestId = newRequest.CancellationRequestId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho RequestCancellationAsync (OrderID: {OrderId})", orderId);
            }
            // ✅ KẾT THÚC GỬI THÔNG BÁO
        }
        public async Task ReviewCancellationAsync(int orderId, ReviewCancelRequestDto request, string staffId)
        {
            // lấy ProductVariant (chứa BaseCost)
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.PlanDetails)
                .Include(o => o.OrderDetails) // Tải lại chi tiết
                    .ThenInclude(od => od.ProductVariant) // Tải kèm Variant
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

            var cancelRequest = await _context.CancellationRequests
                .Where(r => r.OrderId == orderId && r.Status == "Pending")
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            if (cancelRequest == null || order.StatusOrder != 16) // 16 = HOLD
                throw new InvalidOperationException("Đơn hàng này không ở trạng thái 'Tạm Dừng/Chờ'.");

            var previousStatus = await _context.OrderStatuses
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(os => os.Code == cancelRequest.PreviousStatusOrder);

            // 8 = READY_PROD (fallback an toàn nếu không tìm thấy status cũ)
            int previousStatusId = previousStatus?.StatusId ?? 8;

            // ✅ Chuẩn bị biến cho thông báo
            string sellerId = order.SellerUserId;
            string notificationMessage = "";
            string finalStatusString = "";
            int finalStatusId = 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                cancelRequest.ReviewedByStaffId = staffId;
                cancelRequest.ReviewedAt = DateTime.UtcNow;

                if (request.Approved)
                {
                    // === CHẤP NHẬN HỦY ===
                    cancelRequest.Status = "Approved";
                    order.StatusOrder = 17; // 17 = CANCELLED
                    finalStatusId = 17;

                    // 1. Dừng sản xuất: Xóa các PlanDetails
                    foreach (var detail in order.OrderDetails)
                    {
                        if (detail.PlanDetails.Any())
                        {
                            _context.PlanDetails.RemoveRange(detail.PlanDetails);
                        }
                    }

                    // 2. ⭐ LOGIC TÍNH PHÍ HỦY (PENALTY)
                    var inProductionStatuses = new[] { 9, 10, 11, 12, 13, 15 };

                    if (inProductionStatuses.Contains(previousStatusId))
                    {
                        // KỊCH BẢN 1: ĐÃ SẢN XUẤT (Tính phí BaseCost)
                        decimal newTotalCost = 0;
                        foreach (var detail in order.OrderDetails)
                        {
                            if (detail.ProductVariant != null)
                            {
                                newTotalCost += (detail.ProductVariant.BaseCost ?? 0) * detail.Quantity;
                            }
                        }

                        order.TotalCost = newTotalCost; // Ghi đè TotalCost
                        order.ProductionStatus = "TinhBaseCost"; // Đảm bảo trạng thái này được set

                        // ✅ Chuẩn bị thông báo
                        notificationMessage = $"Yêu cầu hủy đơn hàng #{orderId} đã được CHẤP NHẬN. Phí hủy {newTotalCost:C0} đã được áp dụng.";
                        finalStatusString = "CANCELLED_WITH_FEE";
                    }
                    else
                    {
                        // KỊCH BẢN 2: CHƯA SẢN XUẤT (Status 7) (Hủy miễn phí)
                        order.TotalCost = 0;
                        order.ProductionStatus = "CANCELED"; // Trạng thái hủy chung

                        // ✅ Chuẩn bị thông báo
                        notificationMessage = $"Yêu cầu hủy đơn hàng #{orderId} đã được CHẤP NHẬN (miễn phí).";
                        finalStatusString = "CANCELLED";
                    }
                }
                else
                {
                    // === TỪ CHỐI HỦY ===
                    if (string.IsNullOrWhiteSpace(request.RejectionReason))
                        throw new ArgumentException("Lý do từ chối là bắt buộc khi từ chối yêu cầu.");

                    cancelRequest.Status = "Rejected";
                    cancelRequest.RejectionReason = request.RejectionReason;

                    // Khôi phục lại trạng thái cũ
                    order.StatusOrder = previousStatusId;

                    // ✅ Chuẩn bị thông báo
                    finalStatusId = previousStatusId;
                    finalStatusString = previousStatus?.Code ?? "UNKNOWN";
                    notificationMessage = $"Yêu cầu hủy đơn hàng #{orderId} đã bị TỪ CHỐI. Lý do: {request.RejectionReason}";
                }

                // ✅ LƯU DB VÀ COMMIT TRƯỚC
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ BẮT ĐẦU GỬI THÔNG BÁO (SAU KHI COMMIT)
                try
                {
                    // 1. Gửi thông báo (chuông) cho Seller
                    await _notificationService.CreateAndSendNotificationAsync(
                        sellerId,
                        notificationMessage,
                        $"/seller/orders/{orderId}"
                    );

                    // 2. Gửi cập nhật trạng thái real-time cho bất kỳ ai đang xem
                    var orderGroupName = $"order_{orderId}";
                    await _hubContext.Clients.Group(orderGroupName).SendAsync(
                        "OrderStatusChanged",
                        new { orderId = orderId, newStatus = finalStatusString, newStatusId = finalStatusId }
                    );

                    // 3. Gửi sự kiện "request đã được review" cho các Staff dashboards
                    await _hubContext.Clients.Group("StaffNotifications").SendAsync(
                        "CancelRequestReviewed",
                        new { orderId = orderId, newStatus = cancelRequest.Status }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho ReviewCancellationAsync (OrderID: {OrderId})", orderId);
                }
                // ✅ KẾT THÚC GỬI THÔNG BÁO
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}