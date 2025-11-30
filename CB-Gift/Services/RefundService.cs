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
        // --- HÀM TẠO YÊU CẦU HOÀN TIỀN (Request Refund) ---
        // Xử lý cả cấp Order (nhiều item) và cấp OrderDetail (1 item)
        public async Task RequestRefundAsync(RefundRequestDto request, string sellerId)
        {
            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("Yêu cầu hoàn tiền phải chọn ít nhất một chi tiết sản phẩm.");

            // 1. TÌM ORDER GỐC (chỉ để xác thực và PaymentStatus)
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.SellerUserId == sellerId);

            if (order == null)
                throw new KeyNotFoundException($"Không tìm thấy đơn hàng ID: {request.OrderId} hoặc bạn không có quyền truy cập.");

            if (order.PaymentStatus != "Paid")
                throw new InvalidOperationException("Chỉ có thể yêu cầu hoàn tiền cho đơn hàng đã được thanh toán.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. LẶP QUA CÁC ITEMS VÀ TẠO NHIỀU BẢN GHI REFUND
                var newRefunds = new List<Refund>();
                decimal totalRequestedAmount = 0;

                foreach (var itemRequest in request.Items)
                {
                    var orderDetail = order.OrderDetails.FirstOrDefault(od => od.OrderDetailId == itemRequest.OrderDetailId);

                    if (orderDetail == null)
                        throw new KeyNotFoundException($"Không tìm thấy chi tiết sản phẩm ID: {itemRequest.OrderDetailId} trong đơn hàng.");

                    if (itemRequest.RequestedAmount <= 0)
                        throw new ArgumentException($"Số tiền hoàn lại cho sản phẩm {itemRequest.OrderDetailId} phải lớn hơn 0.");

                    // KIỂM TRA TRÙNG LẶP YÊU CẦU PENDING CHO CHI TIẾT NÀY
                    bool alreadyPending = await _context.Refunds.AnyAsync(
                        r => r.OrderDetailId == itemRequest.OrderDetailId && r.Status == "Pending");

                    if (alreadyPending)
                        throw new InvalidOperationException($"Chi tiết sản phẩm ID: {itemRequest.OrderDetailId} đã có yêu cầu hoàn tiền đang chờ.");

                    // TẠO RECORD REFUND MỚI (Cấp OrderDetail)
                    var newRefund = new Refund
                    {
                        OrderId = request.OrderId, // Liên kết với Order gốc
                        OrderDetailId = itemRequest.OrderDetailId, // Khóa ngoại cụ thể
                        RequestedBySellerId = sellerId,
                        Amount = itemRequest.RequestedAmount,
                        Reason = request.Reason,
                        ProofUrl = request.ProofUrl,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };
                    newRefunds.Add(newRefund);
                    totalRequestedAmount += itemRequest.RequestedAmount;
                }

                _context.Refunds.AddRange(newRefunds);

                // 3. CẬP NHẬT TRẠNG THÁI ORDER GỐC
                order.StatusOrder = 16; // 16 = HOLD_RF 

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 4. GỬI THÔNG BÁO (SignalR/Email)
                // ... (Logic SignalR tương tự như cũ) ...
                // 4. GỬI THÔNG BÁO (SignalR/Notification)
                try
                {
                    // Gửi thông báo đến Staff dashboards (SignalR)
                    await _hubContext.Clients.Group("StaffNotifications").SendAsync(
                        "NewRefundRequest",
                        new
                        {
                            orderId = request.OrderId,
                            totalAmount = totalRequestedAmount,
                            itemCount = newRefunds.Count,
                            sellerId = sellerId
                        }
                    );

                    // Thêm notification vào DB cho Staff
                    await _notificationService.CreateAndSendNotificationAsync(
                        "StaffGroup", // Gửi tới nhóm Staff
                        $"Yêu cầu hoàn tiền mới cho Order #{order.OrderCode} ({newRefunds.Count} items). Tổng: {totalRequestedAmount:N0}đ",
                        $"/manager/refund-reprint-review" // Đường dẫn xem yêu cầu
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho RequestRefundAsync (OrderID: {OrderId})", request.OrderId);
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Lỗi khi yêu cầu hoàn tiền cho Order {OrderId}", request.OrderId);
                throw;
            }
        }
        // --- HÀM DUYỆT YÊU CẦU HOÀN TIỀN (Review Refund) --- hàm logic mới
        public async Task ReviewRefundAsync(int refundId, ReviewRefundDto request, string staffId)
        {
            // 1. TÌM BẢN GHI ĐẠI DIỆN VÀ XÁC ĐỊNH ORDER GỐC
            var primaryRefund = await _context.Refunds
                .Include(r => r.Order)
                .Include(r => r.OrderDetail)
                .FirstOrDefaultAsync(r => r.RefundId == refundId);

            if (primaryRefund == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu hoàn tiền (Refund ID đại diện).");

            if (primaryRefund.Status != "Pending")
                throw new InvalidOperationException("Yêu cầu này đã được xử lý trước đó hoặc không ở trạng thái Pending.");

            var order = primaryRefund.Order ?? throw new InvalidOperationException("Refund record missing parent Order.");
            var sellerId = primaryRefund.RequestedBySellerId;

            // 2. LẤY TẤT CẢ CÁC BẢN GHI REFUND PENDING THUỘC ORDER NÀY
            // Chỉ sử dụng OrderId để nhóm
            var refundsInGroup = await _context.Refunds
                .Include(r => r.OrderDetail)
                .Where(r =>
                    r.OrderId == primaryRefund.OrderId && // <-- CHỈ LỌC THEO ORDER ID
                    r.Status == "Pending") // Chỉ duyệt những bản ghi đang Pending trong Order
                .ToListAsync();

            if (!refundsInGroup.Any())
            {
                throw new InvalidOperationException("Không tìm thấy bản ghi Pending nào trong đơn hàng để xử lý.");
            }

            int countAffected = refundsInGroup.Count;

            // Xác định tên mục tiêu cho thông báo
            var targetName = $"tất cả các yêu cầu Refund Pending ({countAffected} mục) thuộc đơn hàng #{order.OrderId}";


            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 3. DUYỆT TỪNG BẢN GHI TRONG NHÓM
                foreach (var refund in refundsInGroup)
                {
                    refund.ReviewedByStaffId = staffId;
                    refund.ReviewedAt = DateTime.UtcNow;

                    if (request.Approved)
                    {
                        // === CHẤP NHẬN HOÀN TIỀN ===
                        refund.Status = "Approved";
                        refund.GatewayRefundId = "MANUAL_REFUND_APPROVED";

                        if (refund.OrderDetail != null)
                        {
                            // Cập nhật trạng thái sản phẩm chi tiết thành REFUND
                            refund.OrderDetail.ProductionStatus = Models.Enums.ProductionStatus.REFUND;
                        }

                        // Cập nhật Order gốc
                        order.StatusOrder = 18;
                    }
                    else
                    {
                        // === TỪ CHỐI HOÀN TIỀN ===
                        if (string.IsNullOrWhiteSpace(request.RejectionReason))
                            throw new ArgumentException("Lý do từ chối là bắt buộc.");

                        refund.Status = "Rejected";
                        refund.StaffRejectionReason = request.RejectionReason;
                        if (refund.OrderDetail != null)
                        {
                            // Cập nhật trạng thái sản phẩm chi tiết thành REFUND
                            refund.OrderDetail.ProductionStatus = Models.Enums.ProductionStatus.QC_DONE;
                        }

                        // Chuyển Order gốc trở lại trạng thái hoạt động trước đó
                        order.StatusOrder = 14;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 4. GỬI THÔNG BÁO (Áp dụng cho toàn bộ các Refund Pending trong đơn hàng)
                try
                {
                    string outcome = request.Approved ? "CHẤP NHẬN" : "TỪ CHỐI";
                    string notificationMessage = $"Hành động {outcome} đã được áp dụng cho {targetName}.";

                    if (!request.Approved && !string.IsNullOrEmpty(request.RejectionReason))
                    {
                        notificationMessage += $" Lý do: {request.RejectionReason}";
                    }

                    // Gửi thông báo (chuông) cho Seller
                    await _notificationService.CreateAndSendNotificationAsync(
                        sellerId,
                        notificationMessage,
                        $"/seller/orders/{order.OrderId}"
                    );

                    // Gửi cập nhật trạng thái real-time
                    var orderGroupName = $"order_{order.OrderId}";
                    await _hubContext.Clients.Group(orderGroupName).SendAsync(
                        "RefundStatusChanged",
                        new
                        {
                            primaryRefundId = primaryRefund.RefundId,
                            newStatus = request.Approved ? "Approved" : "Rejected",
                            orderId = order.OrderId,
                            rejectionReason = request.Approved ? null : request.RejectionReason,
                            affectedCount = countAffected
                        }
                    );
                }
                catch (Exception ex)
                {
                    // Log lỗi thông báo
                    _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho ReviewRefundAsync (Order ID: {OrderId})", order.OrderId);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi duyệt nhóm yêu cầu hoàn tiền dựa trên Order ID: {OrderId}", order.OrderId);
                throw;
            }
        }
        // --- HÀM 3: GET DETAILS (Cho Frontend hiển thị Modal Review) ---
        public async Task<RefundDetailsDto> GetRefundRequestDetailsAsync(int refundId)
        {
            // Lấy Refund record hiện tại
            var refund = await _context.Refunds
                .Include(r => r.Order)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(r => r.RefundId == refundId);

            if (refund == null) throw new KeyNotFoundException("Refund request not found.");

            // Lấy tất cả Refund records liên quan đến Order gốc
            var relatedRefunds = await _context.Refunds
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.ProductVariant.Product)
                .Where(r => r.OrderId == refund.OrderId)
                .ToListAsync();

            // Map các items liên quan đến Order gốc
            var items = relatedRefunds.Select(r => new RefundItemDetailsDto
            {
                OrderDetailId = r.OrderDetailId ?? 0,
                ProductName = r.OrderDetail?.ProductVariant?.Product.ProductName ?? "N/A",
                OriginalPrice = r.OrderDetail?.Price ?? 0,
                RefundAmount = r.Amount,
                Sku = r.OrderDetail?.ProductVariant?.Sku ?? "N/A",
                Quantity = r.OrderDetail?.Quantity ?? 0
            }).ToList();

            // Map kết quả chính
            return new RefundDetailsDto
            {
                RefundId = refund.RefundId,
                OrderId = refund.OrderId,
                OrderCode = refund.Order?.OrderCode ?? "N/A",
                Status = refund.Status,
                TotalRequestedAmount = refund.Amount,
                Reason = refund.Reason,
                ProofUrl = refund.ProofUrl,
                StaffRejectionReason = refund.StaffRejectionReason,
                CreatedAt = refund.CreatedAt,
                // Trả về tất cả các items liên quan đến Order gốc để Manager có cái nhìn tổng quan
                Items = items
            };
        }
        // --- HÀM 4: XÁC THỰC QUYỀN SỞ HỮU ---
        public async Task<bool> IsUserRequesterAsync(int refundId, string userId)
        {
            // Kiểm tra xem có bản ghi Refund nào với ID và SellerId tương ứng không
            return await _context.Refunds
                .AnyAsync(r => r.RefundId == refundId && r.RequestedBySellerId == userId);
        }
        public async Task<PaginatedResult<RefundRequestListDto>> GetReviewRequestsPaginatedAsync(
       string? staffId,
       string? searchTerm,
       string? filterType,
       string? sellerIdFilter,
       string? statusFilter, // <-- THAM SỐ MỚI
       int page,
       int pageSize)
        {
            // 1. TÌM KIẾM DỮ LIỆU GỐC VÀ ÁP DỤNG CÁC BỘ LỌC
            var query = _context.Refunds
                .Include(r => r.Order)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.ProductVariant.Product)
                .AsQueryable();

            // 1.0. Lọc theo Trạng thái (Status Filter)
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter.ToLower() != "all")
            {
                var status = statusFilter.Trim().ToLower();

                // Chỉ lọc các trạng thái hợp lệ
                if (status == "pending" || status == "approved" || status == "rejected")
                {
                    query = query.Where(r => r.Status.ToLower() == status);
                }
                // Nếu statusFilter không hợp lệ, query sẽ không bị lọc thêm và chỉ áp dụng default.
            }
            else // Mặc định hiển thị tất cả các trạng thái có thể Review
            {
                query = query.Where(r => r.Status == "Pending" || r.Status == "Approved" || r.Status == "Rejected");
            }

            // 1.2. Tìm kiếm (Search Term)
            if (!string.IsNullOrEmpty(searchTerm))
             {
                 var term = searchTerm.ToLower();
                 query = query.Where(r =>
                     r.Order.OrderCode.ToLower().Contains(term) ||
                     r.Reason.ToLower().Contains(term));
             }
             if (!string.IsNullOrEmpty(sellerIdFilter))
             {
                 query = query.Where(r => r.RequestedBySellerId == sellerIdFilter);
             }
             // 2. TÍNH TỔNG SỐ LƯỢNG (trước khi phân trang)
             // NOTE: Cần thực hiện grouping trước khi Count nếu bạn muốn đếm số lượng nhóm.
             // Để đơn giản, ta sẽ đếm tổng số bản ghi Refund:
             var totalCount = await query.CountAsync();


             // 3. THỰC HIỆN PHÂN TRANG (SKIP & TAKE)
             var paginatedQuery = query
                 .OrderByDescending(r => r.CreatedAt) // Sắp xếp để lấy dữ liệu mới nhất
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize);

             // 4. MAPPING (Thực hiện Grouping và Mapping DTO)
             // Lấy dữ liệu đã phân trang
             var paginatedRefunds = await paginatedQuery.ToListAsync();

             // Do logic grouping là phức tạp và thường được áp dụng cho toàn bộ tập dữ liệu,
             // Việc grouping trên tập dữ liệu đã phân trang (Take) có thể dẫn đến kết quả không chính xác
             // (ví dụ: nhóm bị chia thành nhiều trang). 
             // Tuy nhiên, nếu bạn muốn hiển thị 1 bản ghi/RefundId, ta chỉ cần map:

             // Khối code dưới đây MOCK logic Grouping lên dữ liệu đã được phân trang (Không lý tưởng, nhưng hoạt động)
             var groupedRequests = paginatedRefunds
                 .GroupBy(r => new { r.OrderId, r.Reason, r.ProofUrl })
                 .Select((group, index) =>
                 {
                     var primaryItem = group.OrderByDescending(r => r.CreatedAt).First();
                     bool isOrderLevel = group.Count() > 1 || primaryItem.OrderDetailId == null;

                     return new RefundRequestListDto
                     {
                         // ... (Logic mapping chi tiết DTO giữ nguyên) ...
                         GroupId = (page - 1) * pageSize + index + 1, // ID duy nhất cho frontend
                         OrderCode = primaryItem.Order?.OrderCode ?? "N/A",
                         Type = "REFUND",
                         TargetLevel = isOrderLevel ? "ORDER-WIDE" : "DETAIL",
                         Status = primaryItem.Status,
                         TotalRequestedAmount = group.Sum(r => r.Amount),
                         CountOfItems = group.Count(),
                         PrimaryRefundId = primaryItem.RefundId,
                         CreatedAt = primaryItem.CreatedAt
                     };
                 })
                 .ToList();


             // 5. TRẢ VỀ PAGINATED RESULT
             return new PaginatedResult<RefundRequestListDto>
             {
                 Items = groupedRequests,
                 Total = totalCount,
                 Page = page,
                 PageSize = pageSize
             };
         }

        
    }
}
