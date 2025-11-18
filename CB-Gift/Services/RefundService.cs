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
    public class RefundService : IRefundService
    {
        private readonly CBGiftDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<RefundService> _logger;

        public RefundService(
            CBGiftDbContext context,
            INotificationService notificationService,
            IHubContext<NotificationHub> hubContext,
            ILogger<RefundService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task RequestRefundAsync(int orderId, SellerRefundRequestDto request, string sellerId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(
                o => o.OrderId == orderId && o.SellerUserId == sellerId);

            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.");

            if (order.PaymentStatus != "Paid")
                throw new InvalidOperationException("Chỉ có thể yêu cầu hoàn tiền cho đơn hàng đã được thanh toán.");

            bool alreadyPending = await _context.Refunds.AnyAsync(
                r => r.OrderId == orderId && r.Status == "Pending");

            if (alreadyPending)
                throw new InvalidOperationException("Đơn hàng này đã có một yêu cầu hoàn tiền đang chờ xử lý.");

            // TẠO YÊU CẦU HOÀN TIỀN MỚI
            var newRefund = new Refund
            {
                OrderId = orderId,
                RequestedBySellerId = sellerId,
                Amount = order.TotalCost ?? 0,
                Reason = request.Reason,
                ProofUrl = request.ProofUrl,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.Refunds.Add(newRefund);
            // chuyển order sang trạng thái HOLD
            order.StatusOrder = 16; // 16 = HOLD (Chuyển sang "Chờ")
            // ✅ LƯU VÀO DB TRƯỚC
            await _context.SaveChangesAsync();

            // ✅ BẮT ĐẦU GỬI THÔNG BÁO (SAU KHI LƯU)
            try
            {
                // Gửi sự kiện "có request mới" cho các Staff dashboards
                // (Giả sử Staff frontend sẽ join group "StaffNotifications")
                await _hubContext.Clients.Group("StaffNotifications").SendAsync(
                    "NewRefundRequest",
                    new
                    {
                        refundId = newRefund.RefundId,
                        orderId = orderId,
                        sellerId = sellerId,
                        amount = newRefund.Amount,
                        reason = newRefund.Reason
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho RequestRefundAsync (OrderID: {OrderId})", orderId);
            }
            // ✅ KẾT THÚC GỬI THÔNG BÁO
        }

        public async Task ReviewRefundAsync(int refundId, StaffReviewRefundDto request, string staffId)
        {
            var refund = await _context.Refunds
                .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
                .ThenInclude(od => od.PlanDetails)
                .FirstOrDefaultAsync(r => r.RefundId == refundId);

            if (refund == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu hoàn tiền.");

            if (refund.Status != "Pending")
                throw new InvalidOperationException("Yêu cầu này đã được xử lý trước đó.");

            var order = refund.Order;

            // ✅ Chuẩn bị biến cho thông báo
            var orderId = refund.OrderId;
            var sellerId = refund.RequestedBySellerId;
            string notificationMessage = "";
            string finalStatusString = "";
            int finalStatusId = order.StatusOrder; // Mặc định là trạng thái hiện tại
            string finalPaymentStatus = order.PaymentStatus;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                refund.ReviewedByStaffId = staffId;
                refund.ReviewedAt = DateTime.UtcNow;

                if (request.Approved)
                {
                    // === CHẤP NHẬN HOÀN TIỀN ===
                    refund.Status = "Approved";
                    refund.GatewayRefundId = "MANUAL_REFUND_APPROVED";

                  /*  foreach (var detail in order.OrderDetails)
                    {
                        if (detail.PlanDetails.Any())
                        {
                            _context.PlanDetails.RemoveRange(detail.PlanDetails);
                        }
                    }*/

                    // Cập nhật Order sang trạng thái cuối
                    order.StatusOrder = 18; // 18 = REFUNDED
                    order.PaymentStatus = "Refunded";
                    order.ProductionStatus = "Refunded";

                    // ✅ Chuẩn bị thông báo
                    notificationMessage = $"Yêu cầu hoàn tiền cho đơn hàng #{orderId} đã được CHẤP NHẬN.";
                    finalStatusString = "REFUNDED";
                    finalStatusId = 18;
                    finalPaymentStatus = "Refunded";
                }
                else
                {
                    // === TỪ CHỐI HOÀN TIỀN ===
                    if (string.IsNullOrWhiteSpace(request.RejectionReason))
                        throw new ArgumentException("Lý do từ chối là bắt buộc.");

                    refund.Status = "Rejected";
                    refund.StaffRejectionReason = request.RejectionReason;
                    order.StatusOrder = 14;
                    // ✅ Chuẩn bị thông báo
                    notificationMessage = $"Yêu cầu hoàn tiền cho đơn hàng #{orderId} đã bị TỪ CHỐI. Lý do: {request.RejectionReason}";
                    finalStatusString = "REFUND_REJECTED"; // Trạng thái ảo để UI biết
                    // finalStatusId và finalPaymentStatus giữ nguyên như ban đầu
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
                        new
                        {
                            orderId = orderId,
                            newStatus = finalStatusString,
                            newStatusId = finalStatusId,
                            newPaymentStatus = finalPaymentStatus // Cập nhật cả trạng thái thanh toán
                        }
                    );

                    // 3. Gửi sự kiện "request đã được review" cho các Staff dashboards
                    await _hubContext.Clients.Group("StaffNotifications").SendAsync(
                        "RefundRequestReviewed",
                        new { refundId = refundId, orderId = orderId, newStatus = refund.Status }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho ReviewRefundAsync (RefundID: {RefundId})", refundId);
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