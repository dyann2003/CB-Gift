using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Identity;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class ReprintService : IReprintService
    {
        private readonly CBGiftDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ReprintService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        // Inject NotificationService vào Constructor
        public ReprintService(
            CBGiftDbContext context,
            IOrderService orderService,
            IMapper mapper,
            ILogger<ReprintService> logger,
            INotificationService notificationService,
            IHubContext<NotificationHub> hubContext,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _orderService = orderService;
            _mapper = mapper;
            _notificationService = notificationService;
            _userManager = userManager;
            _hubContext = hubContext;
            _logger = logger;
        }

        // 1️ USER SUBMIT REPRINT REQUEST
        public async Task SubmitReprintAsync(ReprintSubmitDto dto)
        {
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.OrderDetailId == dto.OriginalOrderDetailId);

            if (orderDetail == null)
                throw new Exception("OrderDetail not found.");

            // Đặt order vào trạng thái chờ xử lý reprint
            orderDetail.Order.StatusOrder = 17;
            orderDetail.ProductionStatus = ProductionStatus.HOLD_RP;

            // Tạo record Reprint
            var reprint = new Reprint
            {
                OriginalOrderDetailId = dto.OriginalOrderDetailId,
                Reason = dto.Reason,
                RequestedBy = dto.RequestedByUserId,
                RequestDate = DateTime.Now,
                Processed = false,
                Status = "Pending",
                ProofUrl = dto.ProofUrl
            };

            _context.Reprints.Add(reprint);
            await _context.SaveChangesAsync();

            // GỬI THÔNG BÁO CHO TẤT CẢ MANAGER
            // BƯỚC 1: Lấy danh sách Manager
            var managers = await _userManager.GetUsersInRoleAsync("Manager");

            // Lấy list ID (Nếu Project dùng Guid thì nhớ convert, nếu string thì giữ nguyên)
            var managerIds = managers.Select(u => u.Id).ToList();

            // BƯỚC 2: Gửi thông báo tuần tự (Sequential)
            // KHÔNG dùng List<Task> và Task.WhenAll để tránh xung đột DbContext
            if (managerIds.Any())
            {
                foreach (var mgrId in managerIds)
                {
                    try
                    {
                        // Dùng await trực tiếp để xử lý xong người này mới đến người kia
                        await _notificationService.CreateAndSendNotificationAsync(
                            mgrId.ToString(), // Chuyển sang string nếu mgrId là Guid
                            $"Yêu cầu in lại MỚI từ đơn hàng #{orderDetail.Order.OrderCode}. Lý do: {dto.Reason}",
                            $"/manager/reprint" // Link tới trang quản lý
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nếu gửi cho 1 manager bị fail, để không ảnh hưởng các manager còn lại
                        _logger.LogError($"Lỗi gửi thông báo cho manager {mgrId}: {ex.Message}");
                    }
                }
            }
        }

        // 2️ MANAGER APPROVE
        public async Task ApproveReprintAsync(ReprintManagerDto dto,string managerId)
        {
            // 1. Kiểm tra input
            if (dto.OriginalOrderDetailIds == null || !dto.OriginalOrderDetailIds.Any())
                throw new Exception("Danh sách sản phẩm trống.");

            // 2. Lấy danh sách OrderDetails theo List ID
            var listOrderDetails = await _context.OrderDetails
                .Include(od => od.Order)
                .ThenInclude(o => o.EndCustomer)
                .Where(od => dto.OriginalOrderDetailIds.Contains(od.OrderDetailId))
                .ToListAsync();

            if (!listOrderDetails.Any())
                throw new Exception("Không tìm thấy sản phẩm nào trong hệ thống.");

            // 3. VALIDATION QUAN TRỌNG
            var firstOrderId = listOrderDetails.First().OrderId;
            if (listOrderDetails.Any(od => od.OrderId != firstOrderId))
            {
                throw new Exception("Lỗi: Không thể duyệt cùng lúc các sản phẩm thuộc các đơn hàng khác nhau.");
            }

            var originalOrder = listOrderDetails.First().Order;

            // 4. Lấy danh sách yêu cầu Reprint tương ứng để update trạng thái
            var listReprints = await _context.Reprints
                .Where(r => dto.OriginalOrderDetailIds.Contains(r.OriginalOrderDetailId))
                .ToListAsync();

            // Update trạng thái Reprint thành Approved
            foreach (var reprint in listReprints)
            {
                reprint.ManagerAcceptedBy = managerId;
                reprint.Processed = true;
                reprint.Status = "Approved";
            }

            // 5. Chuẩn bị danh sách sản phẩm cho đơn mới
            var newDetailsList = new List<OrderDetailCreateRequest>();

            foreach (var item in listOrderDetails)
            {
                var reprintRequest = listReprints.FirstOrDefault(r => r.OriginalOrderDetailId == item.OrderDetailId);
                string reason = reprintRequest?.Reason ?? "N/A";

                newDetailsList.Add(new OrderDetailCreateRequest
                {
                    ProductVariantID = item.ProductVariantId,
                    Quantity = item.Quantity,
                    Price = 0, // Giá 0đ
                    LinkImg = item.LinkImg,
                    NeedDesign = item.NeedDesign,
                    LinkDesign = item.LinkFileDesign,
                    LinkThanksCard = item.LinkThanksCard,
                    Accessory = item.Accessory,
                    Note = $"REPRINT item {item.ProductVariantId}. Lý do: {reason} \n Note cũ: {item.Note}",
                    ProductionStatus = ProductionStatus.READY_PROD
                });
            }

            // 6. Build Order mới
            var makeOrderDto = new MakeOrderDto
            {
                CustomerInfo = new EndCustomerCreateRequest
                {
                    Name = originalOrder.EndCustomer.Name,
                    Phone = originalOrder.EndCustomer.Phone,
                    Email = originalOrder.EndCustomer.Email,
                    Address = originalOrder.EndCustomer.Address,
                    Address1 = originalOrder.EndCustomer.Address1,
                    ZipCode = originalOrder.EndCustomer.Zipcode,
                    ShipState = originalOrder.EndCustomer.ShipState,
                    ShipCity = originalOrder.EndCustomer.ShipCity,
                    ShipCountry = originalOrder.EndCustomer.ShipCountry
                },
                OrderCreate = new OrderCreateRequest
                {
                    CostScan = 0,
                    OrderCode = originalOrder.OrderCode,
                    ToDistrictId = originalOrder.ToDistrictId,
                    ToProvinceId = originalOrder.ToProvinceId,
                    ToWardCode = originalOrder.ToWardCode,
                    ProductionStatus = "Reprint",
                    PaymentStatus = "PAID",
                    ActiveTTS = originalOrder.ActiveTts,
                    Tracking = string.Empty,
                    TotalCost = 0
                },
                OrderDetails = newDetailsList
            };

            // 7. Gọi Service tạo đơn
            string sellerId = originalOrder.SellerUserId;

            var created = await _orderService.MakeOrder(makeOrderDto, sellerId);

            // 8. Cập nhật mã đơn hàng _RE và trạng thái đơn gốc
            var newOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == created.OrderId);

            if (newOrder != null)
            {
                string oldCode = originalOrder.OrderCode;
                int index = oldCode.LastIndexOf("_RE");

                if (index == -1) newOrder.OrderCode = oldCode + "_RE";
                else
                {
                    string suffixStr = oldCode.Substring(index + 3);
                    if (string.IsNullOrEmpty(suffixStr)) newOrder.OrderCode = oldCode + "2";
                    else if (int.TryParse(suffixStr, out int version))
                    {
                        string baseCode = oldCode.Substring(0, index);
                        newOrder.OrderCode = $"{baseCode}_RE{version + 1}";
                    }
                    else newOrder.OrderCode = oldCode + "_RE";
                }

                newOrder.StatusOrder = 8;
                newOrder.ProductionStatus = "Reprint";
                newOrder.PaymentStatus = "Paid";
                newOrder.ActiveTts = originalOrder.ActiveTts;
                newOrder.TotalCost = 0;
                newOrder.ToDistrictId = originalOrder.ToDistrictId;
                newOrder.ToProvinceId = originalOrder.ToProvinceId;
                newOrder.ToWardCode = originalOrder.ToWardCode;

                if (newOrder.OrderDetails != null)
                {
                    foreach (var detail in newOrder.OrderDetails)
                    {
                        detail.Price = 0;
                        detail.ProductionStatus = ProductionStatus.CREATED;
                    }
                }
            }

            // Cập nhật trạng thái đơn gốc
            originalOrder.StatusOrder = 15;

            await _context.SaveChangesAsync();
            // ✨ GỬI THÔNG BÁO CHẤP NHẬN ✨
            try
            {
                string newOrderCode = created.OrderCode; // Giả sử created là OrderDto trả về từ MakeOrder

                // Gửi thông báo (chuông) cho Seller
                await _notificationService.CreateAndSendNotificationAsync(
                    sellerId,
                    $"Yêu cầu In lại cho Order #{originalOrder.OrderCode} đã được CHẤP NHẬN. Đơn In lại mới: #{newOrderCode}",
                    $"/seller/orders/{created.OrderId}" // Link đến đơn mới
                );

                // Gửi cập nhật trạng thái real-time
                await _hubContext.Clients.User(sellerId).SendAsync( // Hoặc Clients.Group($"order_{originalOrder.OrderId}")
                    "ReprintStatusChanged",
                    new { orderId = originalOrder.OrderId, newStatus = "Approved", newOrderId = created.OrderId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho ApproveReprintAsync (Order ID: {OrderId})", originalOrder.OrderId);
            }
        }

        // 3️ MANAGER REJECT
        public async Task RejectReprintAsync(ReprintManagerDto dto,string managerId)
        {
            if (dto.OriginalOrderDetailIds == null || !dto.OriginalOrderDetailIds.Any())
                throw new Exception("Danh sách sản phẩm trống.");

            // 1. Lấy danh sách yêu cầu Reprint cần từ chối
            var listReprints = await _context.Reprints
                .Include(r => r.OriginalOrderDetail)
                .ThenInclude(od => od.Order)
                .Where(r => dto.OriginalOrderDetailIds.Contains(r.OriginalOrderDetailId))
                .ToListAsync();

            if (!listReprints.Any())
                throw new Exception("Không tìm thấy yêu cầu in lại nào.");

            var firstOrderId = listReprints.First().OriginalOrderDetail.OrderId;
            if (listReprints.Any(r => r.OriginalOrderDetail.OrderId != firstOrderId))
            {
                throw new Exception("Lỗi: Vui lòng chỉ xử lý (Từ chối) các sản phẩm thuộc cùng 1 đơn hàng.");
            }
            var sellerId = listReprints.First().OriginalOrderDetail.Order.SellerUserId;
            var originalOrderCode = listReprints.First().OriginalOrderDetail.Order.OrderCode;
            var originalOrderId = listReprints.First().OriginalOrderDetail.Order.OrderId;
            // 2. Duyệt qua từng yêu cầu để cập nhật
            foreach (var reprint in listReprints)
            {
                reprint.Processed = true;
                reprint.ManagerAcceptedBy = managerId; // Lấy từ DTO
                reprint.Status = "Rejected"; //Set trạng thái từ chối
                reprint.StaffRejectionReason = dto.RejectReason;

                if (reprint.OriginalOrderDetail != null)
                {
                    // GIẢ ĐỊNH 1: Sử dụng giá trị số 9 (Dựa trên PRODUCTION_STATUS_MAP phổ biến)
                    reprint.OriginalOrderDetail.ProductionStatus = ProductionStatus.QC_DONE;

                    // HOẶC (Nếu bạn dùng Enum ProductionStatus):
                    // reprint.OriginalOrderDetail.ProductionStatus = (int)ProductionStatus.QC_DONE; 
                }
                // 3. Khôi phục trạng thái đơn hàng gốc (Nếu cần)
                if (reprint.OriginalOrderDetail?.Order != null)
                {
                    reprint.OriginalOrderDetail.Order.StatusOrder = 14;
                }
            }

            await _context.SaveChangesAsync();
            try
            {
                // Gửi thông báo (chuông) cho Seller
                await _notificationService.CreateAndSendNotificationAsync(
                    sellerId,
                    $"Yêu cầu In lại cho Order #{originalOrderCode} đã bị TỪ CHỐI. Lý do: {dto.RejectReason}",
                    $"/seller/order-view/{originalOrderId}"
                );

                // Gửi cập nhật trạng thái real-time
                await _hubContext.Clients.User(sellerId).SendAsync( // Hoặc Clients.Group($"order_{originalOrder.OrderId}")
                    "ReprintStatusChanged",
                    new
                    {
                        orderId = originalOrderId,
                        newStatus = "Rejected",
                        rejectionReason = dto.RejectReason,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho RejectReprintAsync (Order ID: {OrderId})", originalOrderId);
            }
        }
        /* public async Task RequestReprintAsync(SellerReprintRequestDto request, string sellerId)
         {
             // 1. TÌM ORDER GỐC (chỉ để xác thực và lấy chi tiết)
             var order = await _context.Orders
                 .Include(o => o.OrderDetails)
                 .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.SellerUserId == sellerId);

             if (order == null)
                 throw new KeyNotFoundException($"Không tìm thấy đơn hàng ID: {request.OrderId} hoặc bạn không có quyền truy cập.");

             // 2. TẠO CÁC BẢN GHI REPRINT VÀ XÁC THỰC
             using var transaction = await _context.Database.BeginTransactionAsync();
             try
             {
                 foreach (var itemRequest in request.SelectedItems)
                 {
                     var orderDetail = order.OrderDetails.FirstOrDefault(od => od.OrderDetailId == itemRequest.OriginalOrderDetailId);

                     if (orderDetail == null)
                         throw new KeyNotFoundException($"Không tìm thấy chi tiết sản phẩm ID: {itemRequest.OriginalOrderDetailId} trong đơn hàng.");

                     // Kiểm tra yêu cầu PENDING trùng lặp (tránh Seller spam request)
                     bool alreadyPending = await _context.Reprints.AnyAsync(
                         r => r.OriginalOrderDetailId == itemRequest.OriginalOrderDetailId && r.Status == "Pending");

                     if (alreadyPending)
                         throw new InvalidOperationException($"Chi tiết sản phẩm ID: {itemRequest.OriginalOrderDetailId} đã có yêu cầu in lại đang chờ.");

                     // Tạo record Reprint cho TỪNG item
                     var reprint = new Reprint
                     {
                         OriginalOrderDetailId = itemRequest.OriginalOrderDetailId,
                         Reason = request.Reason, // Lý do chi tiết
                         RequestedBy = sellerId,
                         Status = "Pending",
                         ProofUrl = request.ProofUrl, // URL file thiết kế/bằng chứng
                         RequestDate = DateTime.Now
                         // ManagerAcceptedBy, Processed, StaffRejectionReason để null/default
                     };

                     _context.Reprints.Add(reprint);
                 }

                 // 3. CHUYỂN TRẠNG THÁI ORDER GỐC sang HOLD_RP (Hoặc 17 - Reprints Pending)
                 order.StatusOrder = 17;

                 await _context.SaveChangesAsync();
                 await transaction.CommitAsync();

                 // 4. GỬI THÔNG BÁO (Logic SignalR)
                 // ... (Cần gọi hubContext để thông báo cho Manager) ...
             }
             catch (Exception ex)
             {
                 transaction.Rollback();
                 // Ghi log lỗi
                 throw new Exception($"Lỗi khi tạo yêu cầu in lại: {ex.Message}");
             }
         }*/

        public async Task RequestReprintAsync(SellerReprintRequestDto request, string sellerId)
        {
            // 1. TÌM ORDER GỐC (chỉ để xác thực và lấy chi tiết)
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.SellerUserId == sellerId);

            if (order == null)
                throw new KeyNotFoundException($"Không tìm thấy đơn hàng ID: {request.OrderId} hoặc bạn không có quyền truy cập.");

            // 2. TẠO CÁC BẢN GHI REPRINT VÀ XÁC THỰC
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var itemRequest in request.SelectedItems)
                {
                    var orderDetail = order.OrderDetails.FirstOrDefault(od => od.OrderDetailId == itemRequest.OriginalOrderDetailId);

                    if (orderDetail == null)
                        throw new KeyNotFoundException($"Không tìm thấy chi tiết sản phẩm ID: {itemRequest.OriginalOrderDetailId} trong đơn hàng.");

                    // Kiểm tra yêu cầu PENDING trùng lặp (tránh Seller spam request)
                    bool alreadyPending = await _context.Reprints.AnyAsync(
                        r => r.OriginalOrderDetailId == itemRequest.OriginalOrderDetailId && r.Status == "Pending");

                    if (alreadyPending)
                        throw new InvalidOperationException($"Chi tiết sản phẩm ID: {itemRequest.OriginalOrderDetailId} đã có yêu cầu in lại đang chờ.");

                    // 🎯 CẬP NHẬT TRẠNG THÁI CHO ORDERDETAIL
                    // OrderDetail có trường ProductionStatus và bạn có enum/string "HOLD_RP"
                    orderDetail.ProductionStatus = Models.Enums.ProductionStatus.HOLD_RP; // HOẶC string "HOLD_RP" nếu bạn dùng string

                    // Tạo record Reprint cho TỪNG item
                    var reprint = new Reprint
                    {
                        OriginalOrderDetailId = itemRequest.OriginalOrderDetailId,
                        Reason = request.Reason, // Lý do chi tiết
                        RequestedBy = sellerId,
                        Status = "Pending",
                        ProofUrl = request.ProofUrl, // URL file thiết kế/bằng chứng
                        RequestDate = DateTime.Now
                    };

                    _context.Reprints.Add(reprint);
                }

                // 3. CHUYỂN TRẠNG THÁI ORDER GỐC sang HOLD_RP (17 - Reprints Pending)
                // Đây là trạng thái cấp Order, nên nó là một trạng thái chung cho toàn bộ đơn hàng.
                order.StatusOrder = 17;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 4. ✨ GỬI THÔNG BÁO (SignalR/Notification) ✨
                try
                {
                    // Gửi thông báo đến Staff dashboards (SignalR)
                    await _hubContext.Clients.Group("StaffNotifications").SendAsync(
                        "NewReprintRequest",
                        new
                        {
                            orderId = request.OrderId,
                            orderCode = order.OrderCode,
                           // itemCount = newReprintCount,
                            sellerId = sellerId
                        }
                    );

                    // Thêm notification vào DB cho Staff/Manager
                    await _notificationService.CreateAndSendNotificationAsync(
                        "StaffGroup", // Gửi tới nhóm Staff/Manager
                        $"Yêu cầu In lại mới cho Order #{order.OrderCode}.",
                        $"/manager/reprint-review/{request.OrderId}" // Đường dẫn xem yêu cầu
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho RequestReprintAsync (OrderID: {OrderId})", request.OrderId);
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // Ghi log lỗi
                throw new Exception($"Lỗi khi tạo yêu cầu in lại: {ex.Message}");
            }
        }
         /*public async Task<PaginatedResult<ReprintRequestListDto>> GetReviewReprintRequestsPaginatedAsync(
         string? staffId, // Dùng để xác thực (nếu cần)
         string? searchTerm,
         string? filterType,
         string? sellerIdFilter, // Reprint.RequestedBy tương đương
         string? statusFilter,
         int page,
         int pageSize)
         {
             // 1. TÌM KIẾM DỮ LIỆU GỐC VÀ ÁP DỤNG CÁC BỘ LỌC
             // Lấy các bản ghi Reprint
             var query = _context.Reprints
             .Include(r => r.OriginalOrderDetail)
                 .ThenInclude(od => od.Order)
             .Include(r => r.OriginalOrderDetail)
                 .ThenInclude(od => od.ProductVariant)
                     .ThenInclude(pv => pv.Product)
             .AsQueryable();

             // 1.0. Lọc theo Trạng thái (Status Filter) <-- LOGIC LỌC MỚI
             if (!string.IsNullOrEmpty(statusFilter) && statusFilter.ToLower() != "all")
             {
                 var status = statusFilter.Trim().ToLower();

                 if (status == "pending" || status == "approved" || status == "rejected")
                 {
                     query = query.Where(r => r.Status.ToLower() == status);
                 }
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
                     r.OriginalOrderDetail!.Order!.OrderCode.ToLower().Contains(term) || // Phải đảm bảo Order không null
                     r.Reason.ToLower().Contains(term));
             }

             // 1.3. Lọc theo Seller (RequestedBy)
             if (!string.IsNullOrEmpty(sellerIdFilter))
             {
                 query = query.Where(r => r.RequestedBy == sellerIdFilter);
             }

             // 2. TÍNH TỔNG SỐ LƯỢNG (trước khi phân trang)
             var totalCount = await query.CountAsync();

             // 3. THỰC HIỆN PHÂN TRANG (SKIP & TAKE)
             var paginatedReprint = await query
                 .OrderByDescending(r => r.RequestDate) // Sắp xếp theo ngày yêu cầu
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .ToListAsync();

             // 4. MAPPING DTO
             // Với Reprint, ta có thể không cần grouping phức tạp như Refund vì nó thường là 1:1 với OrderDetail
             var mappedRequests = paginatedReprint
                 .Select((item, index) => new ReprintRequestListDto
                 {
                     GroupId = (page - 1) * pageSize + index + 1, // ID duy nhất cho frontend
                     OrderId = item.OriginalOrderDetail!.OrderId,
                     OrderCode = item.OriginalOrderDetail.Order?.OrderCode ?? "N/A",
                     Type = "REPRINT",
                     TargetLevel = "DETAIL",
                     Status = item.Status,
                     ReasonSummary = item.Reason,
                     OriginalOrderDetailId = item.OriginalOrderDetailId,
                     ProductName = item.OriginalOrderDetail.ProductVariant?.Product?.ProductName ?? "N/A",
                     PrimaryReprintId = item.Id,
                     CreatedAt = item.RequestDate
                 })
                 .ToList();

             // 5. TRẢ VỀ PAGINATED RESULT
             return new PaginatedResult<ReprintRequestListDto>
             {
                 Items = mappedRequests,
                 Total = totalCount,
                 Page = page,
                 PageSize = pageSize
             };
         } */

        public async Task<PaginatedResult<ReprintRequestListDto>> GetReviewReprintRequestsPaginatedAsync(
        string? staffId,
        string? searchTerm,
        string? filterType, // Không dùng nhưng giữ lại để tương thích
        string? sellerIdFilter,
        string? statusFilter, // Dùng để lọc trạng thái
        int page,
        int pageSize)
        {
            // 1. TÌM KIẾM DỮ LIỆU GỐC VÀ ÁP DỤNG CÁC BỘ LỌC (TRÊN TỪNG BẢN GHI)
            var query = _context.Reprints
                .Include(r => r.OriginalOrderDetail)
                    .ThenInclude(od => od.Order)
                .Include(r => r.OriginalOrderDetail)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .AsQueryable();

            // 1.1. Lọc theo Trạng thái (Status Filter)
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter.ToLower() != "all")
            {
                var status = statusFilter.Trim().ToLower();
                if (status == "pending" || status == "approved" || status == "rejected")
                {
                    query = query.Where(r => r.Status.ToLower() == status);
                }
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
                    r.OriginalOrderDetail!.Order!.OrderCode.ToLower().Contains(term) ||
                    r.Reason.ToLower().Contains(term) ||
                    (r.OriginalOrderDetail.ProductVariant != null && r.OriginalOrderDetail.ProductVariant.Product != null && r.OriginalOrderDetail.ProductVariant.Product.ProductName.ToLower().Contains(term)));
            }

            // 1.3. Lọc theo Seller (RequestedBy)
            if (!string.IsNullOrEmpty(sellerIdFilter))
            {
                query = query.Where(r => r.RequestedBy == sellerIdFilter);
            }

            // 2. GROUPING VÀ PHÂN TRANG

            // 2.1. Nhóm các yêu cầu theo OrderId + Reason + ProofUrl + Status
            // Logic nhóm này giúp gộp các yêu cầu Reprint có cùng nguyên nhân/trạng thái/đơn hàng lại với nhau
            var groupedQuery = query
                .GroupBy(r => new { r.OriginalOrderDetail!.OrderId, r.Reason, r.ProofUrl, r.Status })
                .Select(group => new
                {
                    // Thông tin chung của nhóm
                    OrderId = group.Key.OrderId,
                    Status = group.Key.Status,
                    ReasonSummary = group.Key.Reason,

                    // Dữ liệu tổng hợp
                    PrimaryReprintId = group.Max(r => r.Id), // ID đại diện (Reprint Id lớn nhất)
                    CreatedAt = group.Min(r => r.RequestDate), // Ngày tạo sớm nhất
                    CountOfItems = group.Count(), // Số lượng OrderDetail bị ảnh hưởng

                    // Lấy thông tin Order Code/Product Name từ bản ghi đầu tiên/đại diện
                    OrderCode = group.Max(r => r.OriginalOrderDetail.Order!.OrderCode),

                    // Lấy OriginalOrderDetailId của một item trong nhóm để truyền cho Detail API (fallback)
                    OriginalOrderDetailId = group.Select(r => r.OriginalOrderDetailId).FirstOrDefault(),

                    // Lấy Tên sản phẩm đại diện (Nếu chỉ có 1 item, hiển thị tên sản phẩm đó)
                    ProductRepresentativeName = group.Count() == 1 ? group.Max(r => r.OriginalOrderDetail.ProductVariant.Product.ProductName) : "Multiple Items"
                })
                .AsQueryable();

            // 2.2. Tính Tổng số nhóm
            var totalCount = await groupedQuery.CountAsync();

            // 2.3. Sắp xếp và Phân trang trên nhóm
            var paginatedGroupedQuery = groupedQuery
                .OrderByDescending(g => g.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var paginatedGroups = await paginatedGroupedQuery.ToListAsync();

            // 3. MAPPING DTO
            var mappedRequests = paginatedGroups
                .Select((group, index) => new ReprintRequestListDto
                {
                    GroupId = (page - 1) * pageSize + index + 1,
                    OrderId = group.OrderId,
                    OrderCode = group.OrderCode,
                    Type = "REPRINT",
                    // TargetLevel là ORDER-WIDE nếu có nhiều hơn 1 item hoặc nếu cần phân biệt rõ ràng
                    TargetLevel = group.CountOfItems > 1 ? "ORDER-WIDE" : "DETAIL",
                    Status = group.Status,
                    ReasonSummary = group.ReasonSummary,
                    CountOfItems = group.CountOfItems,
                    PrimaryReprintId = group.PrimaryReprintId,
                    CreatedAt = group.CreatedAt,

                    // Product Name đại diện
                    ProductName = group.CountOfItems > 1 ? $"Grouped ({group.CountOfItems} items)" : group.ProductRepresentativeName
                })
                .ToList();

            // 4. TRẢ VỀ PAGINATED RESULT
            return new PaginatedResult<ReprintRequestListDto>
            {
                Items = mappedRequests,
                Total = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<ReprintDetailsDto?> GetReprintDetailsAsync(int reprintId)
        {
            // 1. TÌM BẢN GHI ĐẠI DIỆN VÀ THÔNG TIN NHÓM
            var primaryReprint = await _context.Reprints
                .Include(r => r.OriginalOrderDetail)
                    .ThenInclude(od => od.Order)
                .Include(r => r.OriginalOrderDetail)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(r => r.Id == reprintId);

            if (primaryReprint == null) return null;

            var order = primaryReprint.OriginalOrderDetail?.Order;

            // 2. XÁC ĐỊNH VÀ LẤY TẤT CẢ CÁC BẢN GHI REPRINT THUỘC CÙNG NHÓM (Order, Reason, ProofUrl, Status)
            var groupKey = new
            {
                OrderId = primaryReprint.OriginalOrderDetail?.OrderId,
                primaryReprint.Reason,
                primaryReprint.ProofUrl,
                primaryReprint.Status
            };

            // Lấy TẤT CẢ các bản ghi Reprint thuộc nhóm này (kể cả đã Approved/Rejected)
            var allGroupedReprints = await _context.Reprints
                .Include(r => r.OriginalOrderDetail)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Where(r =>
                    r.OriginalOrderDetail!.OrderId == groupKey.OrderId &&
                    r.Reason == groupKey.Reason &&
                    r.ProofUrl == groupKey.ProofUrl &&
                    r.Status == groupKey.Status)
                .ToListAsync();

            // 3. TỔNG HỢP DANH SÁCH ITEMS (OrderDetail)
            var requestedItems = allGroupedReprints.Select(r => new ReprintItemDto
            {
                OrderDetailId = r.OriginalOrderDetailId,
                ProductName = r.OriginalOrderDetail.ProductVariant?.Product?.ProductName ?? "N/A",
                SKU = r.OriginalOrderDetail.ProductVariant?.Sku ?? "N/A",
                Quantity = r.OriginalOrderDetail.Quantity,
                ReprintSelected = true // Luôn là true vì đã có record Reprint
            }).ToList();


            // 4. MAPPING DTO CHI TIẾT
            return new ReprintDetailsDto
            {
                Id = primaryReprint.Id,
                OrderId = groupKey.OrderId ?? 0,
                OrderCode = order?.OrderCode ?? "N/A",
                Status = primaryReprint.Status,
                Reason = primaryReprint.Reason,

                ProofUrl = primaryReprint.ProofUrl,

                RejectionReason = primaryReprint.StaffRejectionReason,
                RequestDate = primaryReprint.RequestDate,

                // 🎯 TRUYỀN TOÀN BỘ DANH SÁCH ITEMS THUỘC NHÓM
                RequestedItems = requestedItems
            };
        }
    }
}