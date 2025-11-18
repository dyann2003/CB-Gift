using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services.IService; 
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CB_Gift.Hubs;

namespace CB_Gift.Services
{
    public class DesignerTaskService : IDesignerTaskService
    {
        private readonly CBGiftDbContext _context;
        private readonly IImageManagementService _imageService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<DesignerTaskService> _logger;
        public DesignerTaskService(
            CBGiftDbContext context,
            IImageManagementService imageService,
            INotificationService notificationService,
            IHubContext<NotificationHub> hubContext,
            ILogger<DesignerTaskService> logger)
        {
            _context = context;
            _imageService = imageService;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<IEnumerable<DesignTaskDto>> GetAssignedTasksAsync(string designerId)
        {
            // Định nghĩa các trạng thái ProductionStatus liên quan đến công việc của designer
            var designStatuses = new[]
            {
                ProductionStatus.NEED_DESIGN,
                ProductionStatus.DESIGNING,
                ProductionStatus.CHECK_DESIGN,
                ProductionStatus.DESIGN_REDO,
                ProductionStatus.READY_PROD,
                ProductionStatus.IN_PROD,
                ProductionStatus.QC_DONE,
                ProductionStatus.FINISHED,
                ProductionStatus.QC_FAIL,
                ProductionStatus.PROD_REWORK,
                ProductionStatus.PACKING,
                ProductionStatus.HOLD,
                ProductionStatus.CANCELLED
            };
            // Định nghĩa các trạng thái OrderStatus liên quan đến thiết kế
            var designOrderStatuses = new[] { 3, 4, 5, 6 }; // StatusOrder là kiểu int
            var tasks = await _context.OrderDetails
            .Include(od => od.Order)
            .Include(od => od.ProductVariant.Product)
            .Where(od => od.AssignedDesignerUserId == designerId &&
                         od.NeedDesign == true &&
                         od.ProductionStatus.HasValue &&
                         designStatuses.Contains(od.ProductionStatus.Value) &&
                         designOrderStatuses.Contains(od.Order.StatusOrder))
            .Select(od => new DesignTaskDto
            {
                OrderDetailId = od.OrderDetailId,
                OrderId = od.OrderId,
                OrderCode = od.Order.OrderCode,
                ProductName = od.ProductVariant.Product.ProductName,
                ProductDescribe = od.ProductVariant.Product.Describe,
                ProductTemplate = od.ProductVariant.Product.Template,
                OrderStatus = od.Order.StatusOrder,
                Quantity = od.Quantity,
                LinkImg = od.LinkImg,
                LinkThankCard = od.LinkThanksCard,
                LinkFileDesign = od.LinkFileDesign,
                Note = od.Note,
                AssignedAt = od.AssignedAt,
                ProductionStatus = od.ProductionStatus.ToString(),

                //Truy vấn con để lấy lý do
                Reason = (od.ProductionStatus == ProductionStatus.DESIGN_REDO )
                    ? (from log in _context.OrderDetailLogs
                       where log.OrderDetailId == od.OrderDetailId &&
                             (log.EventType == "DESIGN_REJECTED")
                       orderby log.CreatedAt descending
                       select log.Reason
                      ).FirstOrDefault()
                    : null, // Nếu không phải trạng thái bị từ chối, Reason là null
                ProductDetails = od.ProductVariant != null ? new ProductDetails
                        {
                            ProductVariantId = od.ProductVariant.ProductVariantId.ToString(),
                            LengthCm = od.ProductVariant.LengthCm,
                            HeightCm = od.ProductVariant.HeightCm,
                            WidthCm = od.ProductVariant.WidthCm,
                            ThicknessMm = od.ProductVariant.ThicknessMm,
                            SizeInch = od.ProductVariant.SizeInch,
                            Layer = od.ProductVariant.Layer,
                            CustomShape = od.ProductVariant.CustomShape,
                            Sku = od.ProductVariant.Sku
                        } : null
                    })
                    .AsNoTracking()
                    .ToListAsync();
                return tasks;
        }

        // DesignerTaskService.cs
        public async Task<bool> UploadDesignFileAsync(int orderDetailId, string designerId, UploadDesignDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Order)
                    .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

                if (orderDetail == null || orderDetail.AssignedDesignerUserId != designerId)
                {
                    return false;
                }

                var currentStatus = orderDetail.ProductionStatus ?? ProductionStatus.NEED_DESIGN;

                // Logic nghiệp vụ: Chỉ cho phép upload khi đang ở DESIGNING hoặc DESIGN_REDO
                if (currentStatus != ProductionStatus.DESIGNING && currentStatus != ProductionStatus.DESIGN_REDO && currentStatus != ProductionStatus.CHECK_DESIGN)
                {
                    throw new InvalidOperationException($"Cannot upload design file. Current status is {currentStatus}. Must be DESIGNING or DESIGN_REDO.");
                }

                // ********** Logic xác định finalFileUrl **********
                string finalFileUrl;
                if (dto.DesignFile != null && dto.DesignFile.Length > 0)
                {
                    // TRƯỜNG HỢP 1: Upload file mới từ máy tính
                    await using var stream = dto.DesignFile.OpenReadStream();
                    var uploadedFile = await _imageService.UploadImageForUserAsync(
                        stream,
                        dto.DesignFile.FileName,
                        designerId
                    );
                    finalFileUrl = uploadedFile.SecureUrl;
                }
                else if (!string.IsNullOrEmpty(dto.FileUrl))
                {
                    // TRƯỜNG HỢP 2: Sử dụng URL của file đã có sẵn
                    finalFileUrl = dto.FileUrl;
                }
                else
                {
                    // Cả hai trường đều trống -> Lỗi
                    throw new ArgumentException("Phải cung cấp File mới hoặc URL File đã có.");
                }

                // 3. LOGIC UPSERT (UPDATE HOẶC INSERT) VÀO OrderDetailDesign
                // Chúng ta tìm kiếm bản ghi nháp hiện tại (IsFinal = false)
                var existingDesignRecord = await _context.OrderDetailDesigns
                    .FirstOrDefaultAsync(odd => odd.OrderDetailId == orderDetailId);

                if (existingDesignRecord != null)
                {
                    // TRƯỜNG HỢP 1: UPDATE bản nháp đã tồn tại
                    existingDesignRecord.FileUrl = finalFileUrl;
                    existingDesignRecord.Note = dto.Note;
                    existingDesignRecord.CreatedAt = DateTime.UtcNow; // Cập nhật thời gian upload
                }
                else
                {
                    // TRƯỜNG HỢP 2: INSERT bản nháp mới
                    var newDesignRecord = new OrderDetailDesign
                    {
                        OrderDetailId = orderDetailId,
                        DesignerUserId = designerId,
                        FileUrl = finalFileUrl,
                        Note = dto.Note,
                        IsFinal = false, // Bản ghi mới này là bản nháp
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.OrderDetailDesigns.Add(newDesignRecord);
                }

                // 4. Cập nhật lại LinkFileDesign và ProductionStatus trong OrderDetail
                orderDetail.LinkFileDesign = finalFileUrl;

                // CẬP NHẬT TRẠNG THÁI ORDER DETAIL: DESIGNING/DESIGN_REDO -> CHECK_DESIGN
                orderDetail.ProductionStatus = ProductionStatus.CHECK_DESIGN;

                // ✅ BẮT ĐẦU GỬI THÔNG BÁO
                try
                {
                    var sellerId = orderDetail.Order.SellerUserId;
                    var orderId = orderDetail.OrderId;

                    // 1. Gửi thông báo (chuông) cho Seller
                    await _notificationService.CreateAndSendNotificationAsync(
                        sellerId,
                        $"Designer đã upload thiết kế cho mục #{orderDetailId} (Đơn hàng #{orderId}). Vui lòng duyệt.",
                        $"/seller/orders/{orderId}"
                    );

                    // 2. Gửi cập nhật real-time
                    var orderGroupName = $"order_{orderId}";
                    await _hubContext.Clients.Group(orderGroupName).SendAsync(
                        "OrderStatusChanged",
                        new { orderId = orderId, orderDetailId = orderDetailId, newStatus = ProductionStatus.CHECK_DESIGN.ToString() }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi gửi thông báo SignalR cho UploadDesignFileAsync");

                }
                // ✅ KẾT THÚC GỬI THÔNG BÁO

                // 5. Lưu tất cả thay đổi vào database
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                // Khi xảy ra Exception, Rollback transaction
                await transaction.RollbackAsync();
                throw;
            }
        }
        /// <summary>
        /// Cập nhật trạng thái thiết kế của một chi tiết đơn hàng (OrderDetail) 
        /// và kiểm tra điều kiện để chuyển trạng thái tổng thể của Order.
        /// </summary>
        /// <param name="orderDetailId">ID của chi tiết đơn hàng cần cập nhật.</param>
        /// <param name="newStatus">Trạng thái ProductionStatus mới (dạng Enum).</param>
        /// <returns>True nếu cập nhật thành công.</returns>
        /*
        public async Task<bool> UpdateStatusAsync(int orderDetailId, ProductionStatus newStatus)
        {
            // 1. Tải chi tiết đơn hàng VÀ Order cha (BẮT BUỘC INCLUDE)
            var orderDetail = await _context.OrderDetails
                // Bắt buộc phải Include Order để có thể thay đổi thuộc tính StatusOrder
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (orderDetail == null)
            {
                return false;
            }

            // Lấy trạng thái Production hiện tại
            var currentStatus = orderDetail.ProductionStatus;
            // var currentStatus = orderDetail.ProductionStatus ?? ProductionStatus.NEED_DESIGN;

            bool isStatusChanged = false; // Đặt mặc định là false

            // 2. Logic Chuyển đổi Trạng thái

            // Logic 1: GỬI KIỂM TRA QA (DESIGNING -> CHECK_DESIGN)
            if (newStatus == ProductionStatus.CHECK_DESIGN)
            {
                // Kiểm tra xem trạng thái hiện tại có hợp lệ để chuyển sang CHECK_DESIGN không
                if (currentStatus != ProductionStatus.DESIGNING && currentStatus != ProductionStatus.DESIGN_REDO && currentStatus != ProductionStatus.CHECK_DESIGN)
                {
                    throw new InvalidOperationException($"Invalid transition from {currentStatus} to {newStatus} for Order Detail {orderDetailId}.");
                }

                // A. Cập nhật ProductionStatus của chi tiết hiện tại (Chỉ cập nhật nếu trạng thái thay đổi)
                if (currentStatus != ProductionStatus.CHECK_DESIGN)
                {
                    orderDetail.ProductionStatus = newStatus;
                    isStatusChanged = true;
                }

                // B. <<< LOGIC KIỂM TRA VÀ CẬP NHẬT TRẠNG THÁI ORDER CHỦ >>>

                // 1. Tải TẤT CẢ OrderDetails của Order cha cần design
                var allDesignDetails = await _context.OrderDetails
                    .Where(od => od.OrderId == orderDetail.OrderId && od.NeedDesign == true)
                    .Select(od => od.ProductionStatus)
                    .ToListAsync();

                // 2. Kiểm tra xem TẤT CẢ các chi tiết đã chuyển sang CHECK_DESIGN chưa.
                // Điều kiện: Tất cả phải có giá trị (HasValue) và giá trị phải là CHECK_DESIGN (5).
                // Cần đảm bảo chi tiết hiện tại đã được cập nhật ProductionStatus trước khi kiểm tra.
                // Dùng `allDesignDetails.Count == allDesignDetails.Count(status => status.HasValue && status.Value == ProductionStatus.CHECK_DESIGN)` 
                // để chắc chắn rằng số lượng khớp.

                var allDesignChecked = allDesignDetails.All(status =>
                    status.HasValue && status.Value == ProductionStatus.CHECK_DESIGN
                );

                if (allDesignChecked && orderDetail.Order.StatusOrder != 5) // Chỉ cập nhật nếu trạng thái chưa phải là 5
                {
                    // C. Cập nhật trạng thái Order cha (StatusOrder 5 = Check Design)
                    orderDetail.Order.StatusOrder = 5;
                    isStatusChanged = true;
                }
            }
            // -------------------------------------------------------------------------------------------------------------
            // Logic 2: ACCEPT DESIGN (NEED_DESIGN -> DESIGNING)
            else if (newStatus == ProductionStatus.DESIGNING && currentStatus == ProductionStatus.NEED_DESIGN)
            {
                orderDetail.ProductionStatus = newStatus;
                isStatusChanged = true; // Chi tiết đã thay đổi, cần lưu

                // <<< LOGIC MỚI: KIỂM TRA VÀ CẬP NHẬT TRẠNG THÁI ORDER CHỦ LÊN DESIGNING (3) >>>

                // 1. Tải TẤT CẢ OrderDetails của Order cha cần design
                // Lưu ý: Chúng ta cần tải lại hoặc cẩn thận với chi tiết hiện tại.
                // Cách an toàn nhất là tải lại tất cả ProductionStatus SAU KHI chi tiết hiện tại đã được gán (nhưng chưa lưu DB).

                var orderId = orderDetail.OrderId;
                var allDetailsStatuses = await _context.OrderDetails
                    .Where(od => od.OrderId == orderId && od.NeedDesign == true)
                    .Select(od => new { od.OrderDetailId, od.ProductionStatus })
                    .ToListAsync();

                // Cập nhật trạng thái mới cho chi tiết hiện tại trong danh sách tạm thời (nếu tìm thấy)
                var currentDetailStatus = allDetailsStatuses.FirstOrDefault(od => od.OrderDetailId == orderDetailId);
                if (currentDetailStatus != null)
                {
                    // Vì chúng ta không thể thay đổi giá trị trong List<AnonymousType>, 
                    // chúng ta sẽ kiểm tra thủ công.

                    // 2. Kiểm tra xem TẤT CẢ các chi tiết đã chuyển sang DESIGNING chưa.
                    // Điều kiện: Chi tiết hiện tại phải là DESIGNING (newStatus), và TẤT CẢ
                    // các chi tiết khác phải là DESIGNING HOẶC newStatus.

                    // Sử dụng một cờ để kiểm tra:
                    bool allDesignersAssigned = true;
                    foreach (var detailStatus in allDetailsStatuses)
                    {
                        if (detailStatus.OrderDetailId == orderDetailId)
                        {
                            // Chi tiết hiện tại phải là DESIGNING
                            if (newStatus != ProductionStatus.DESIGNING)
                            {
                                allDesignersAssigned = false;
                                break;
                            }
                        }
                        else
                        {
                            // Các chi tiết khác phải là DESIGNING
                            if (detailStatus.ProductionStatus.GetValueOrDefault(ProductionStatus.NEED_DESIGN) != ProductionStatus.DESIGNING)
                            {
                                allDesignersAssigned = false;
                                break;
                            }
                        }
                    }

                    // Cách kiểm tra đơn giản và hiệu quả hơn (Giả sử DESIGNING là 3)
                    // Lấy ra tất cả trạng thái, bao gồm cả trạng thái MỚI của chi tiết hiện tại.
                    var allStatuses = allDetailsStatuses.Select(s => s.ProductionStatus).ToList();

                    // Thay thế trạng thái cũ của chi tiết hiện tại bằng trạng thái mới
                    int indexToUpdate = allStatuses.FindIndex(status =>
                        status.GetValueOrDefault(ProductionStatus.NEED_DESIGN) == currentStatus && currentDetailStatus.OrderDetailId == orderDetailId
                    );
                    if (indexToUpdate != -1)
                    {
                        allStatuses[indexToUpdate] = newStatus;
                    }

                    bool allAreDesigning = allStatuses.All(status =>
                        status.HasValue && status.Value == ProductionStatus.DESIGNING
                    );

                    if (allAreDesigning && orderDetail.Order.StatusOrder < 4) // Giả sử StatusOrder 3 = Designing
                    {
                        // C. Cập nhật trạng thái Order cha
                        orderDetail.Order.StatusOrder = 4;
                        isStatusChanged = true;
                    }
                }
            }
            // -------------------------------------------------------------------------------------------------------------
            // Logic 3: START REDO (DESIGN_REDO -> DESIGNING)
            else if (newStatus == ProductionStatus.DESIGNING && currentStatus == ProductionStatus.DESIGN_REDO)
            {
                orderDetail.ProductionStatus = newStatus;
                isStatusChanged = true;
                // KHÔNG cập nhật Order cha ở đây vì Order đã ở trạng thái Designing (3) hoặc cao hơn.
            }
            else if (currentStatus == newStatus)
            {
                // Trạng thái không thay đổi
                isStatusChanged = false;
            }
            else
            {
                // Chuyển đổi trạng thái không hợp lệ theo logic nghiệp vụ
                throw new InvalidOperationException($"Invalid transition from {currentStatus} to {newStatus} for Order Detail {orderDetailId}.");
            }

            // 3. Lưu thay đổi vào Database
            if (isStatusChanged)
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }
        */
        /// <summary> hàm UpdateStatus mới có SignalR
        /// Cập nhật trạng thái thiết kế của một chi tiết đơn hàng (OrderDetail)
        /// và kiểm tra điều kiện để chuyển trạng thái tổng thể của Order.
        /// ĐÃ TÍCH HỢP NOTIFICATION VÀ SIGNALR.
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int orderDetailId, ProductionStatus newStatus)
        {
            // 1. Tải chi tiết đơn hàng VÀ Order cha
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order) // Bắt buộc Include Order
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (orderDetail == null)
            {
                return false;
            }

            var currentStatus = orderDetail.ProductionStatus;
            bool isStatusChanged = false;

            // Lấy thông tin cần thiết cho thông báo
            var orderId = orderDetail.OrderId;
            var sellerId = orderDetail.Order.SellerUserId;

            // 2. Logic Chuyển đổi Trạng thái

            // Logic 1: GỬI KIỂM TRA (DESIGNING / DESIGN_REDO -> CHECK_DESIGN)
            // Đây là khi Designer upload file xong và gửi cho Seller duyệt
            if (newStatus == ProductionStatus.CHECK_DESIGN)
            {
                if (currentStatus != ProductionStatus.DESIGNING && currentStatus != ProductionStatus.DESIGN_REDO && currentStatus != ProductionStatus.CHECK_DESIGN)
                {
                    throw new InvalidOperationException($"Invalid transition from {currentStatus} to {newStatus} for Order Detail {orderDetailId}.");
                }

                if (currentStatus != ProductionStatus.CHECK_DESIGN)
                {
                    orderDetail.ProductionStatus = newStatus;
                    isStatusChanged = true;

                    // ✅ GỬI THÔNG BÁO (Chi tiết)
                    await SendNotificationAndRealtimeUpdateAsync(
                        sellerId,
                        $"Designer đã upload thiết kế cho mục #{orderDetailId} (Đơn hàng #{orderId}). Vui lòng duyệt.",
                        $"/seller/orders/{orderId}",
                        orderId,
                        orderDetailId,
                        newStatus
                    );
                }

                // B. <<< LOGIC KIỂM TRA VÀ CẬP NHẬT TRẠNG THÁI ORDER CHỦ (ĐÃ SỬA LẠI) >>>

                // 1. Tải TẤT CẢ trạng thái ProductionStatus của các mục cần design trong Order này
                var allDesignDetailStatuses = await _context.OrderDetails
                    .Where(od => od.OrderId == orderId && od.NeedDesign == true)
                    .Select(od => new { od.OrderDetailId, od.ProductionStatus })
                    .ToListAsync();

                // 2. Tạo danh sách trạng thái "giả định" (nếu item này được cập nhật)
                var hypotheticalStatuses = allDesignDetailStatuses.Select(s =>
                    (s.OrderDetailId == orderDetailId) ? newStatus : s.ProductionStatus
                ).ToList();

                // 3. Kiểm tra xem TẤT CẢ đã là CHECK_DESIGN chưa
                var allDesignChecked = hypotheticalStatuses.All(status =>
                    status.HasValue && status.Value == ProductionStatus.CHECK_DESIGN
                );

                if (allDesignChecked && orderDetail.Order.StatusOrder != 5) // 5 = Check Design
                {
                    orderDetail.Order.StatusOrder = 5;
                    isStatusChanged = true;

                    // ✅ GỬI THÔNG BÁO (Tổng thể Order)
                    await SendNotificationAndRealtimeUpdateAsync(
                        sellerId,
                        $"Toàn bộ đơn hàng #{orderId} đã sẵn sàng để bạn duyệt thiết kế.",
                        $"/seller/orders/{orderId}",
                        orderId,
                        null, // null ID chi tiết nghĩa là thông báo cho cả đơn hàng
                        newStatus,
                        true // Đánh dấu đây là thông báo cấp độ Order
                    );
                }
            }
            // -------------------------------------------------------------------------------------------------------------
            // Logic 2: ACCEPT DESIGN (NEED_DESIGN -> DESIGNING)
            // Đây là khi Designer nhấn "Nhận việc"
            else if (newStatus == ProductionStatus.DESIGNING && currentStatus == ProductionStatus.NEED_DESIGN)
            {
                orderDetail.ProductionStatus = newStatus;
                isStatusChanged = true;

                // ✅ GỬI THÔNG BÁO (Chi tiết)
                await SendNotificationAndRealtimeUpdateAsync(
                    sellerId,
                    $"Designer đã BẮT ĐẦU thiết kế cho mục #{orderDetailId} (Đơn hàng #{orderId}).",
                    $"/seller/orders/{orderId}",
                    orderId,
                    orderDetailId,
                    newStatus
                );

                // B. <<< LOGIC KIỂM TRA VÀ CẬP NHẬT TRẠNG THÁI ORDER CHỦ >>>

                // Tải TẤT CẢ trạng thái
                var allDesignDetailStatuses = await _context.OrderDetails
                    .Where(od => od.OrderId == orderId && od.NeedDesign == true)
                    .Select(od => new { od.OrderDetailId, od.ProductionStatus })
                    .ToListAsync();

                // Tạo danh sách trạng thái "giả định"
                var hypotheticalStatuses = allDesignDetailStatuses.Select(s =>
                    (s.OrderDetailId == orderDetailId) ? newStatus : s.ProductionStatus
                ).ToList();

                // Kiểm tra xem TẤT CẢ đã *ít nhất* là DESIGNING (đang làm hoặc đã xong)
                bool allAreInProgress = hypotheticalStatuses.All(status =>
                    status.HasValue && status.Value >= ProductionStatus.DESIGNING
                );

                if (allAreInProgress && orderDetail.Order.StatusOrder < 4) // 4 = Designing
                {
                    orderDetail.Order.StatusOrder = 4;
                    isStatusChanged = true;

                    // ✅ GỬI THÔNG BÁO (Tổng thể Order)
                    await SendNotificationAndRealtimeUpdateAsync(
                       sellerId,
                       $"Toàn bộ đơn hàng #{orderId} đã được designer tiếp nhận.",
                       $"/seller/orders/{orderId}",
                       orderId,
                       null,
                       newStatus,
                       true
                   );
                }
            }
            // -------------------------------------------------------------------------------------------------------------
            // Logic 3: START REDO (DESIGN_REDO -> DESIGNING)
            // Đây là khi Designer làm lại
            else if (newStatus == ProductionStatus.DESIGNING && currentStatus == ProductionStatus.DESIGN_REDO)
            {
                orderDetail.ProductionStatus = newStatus;
                isStatusChanged = true;

                // ✅ GỬI THÔNG BÁO (Chi tiết)
                await SendNotificationAndRealtimeUpdateAsync(
                    sellerId,
                    $"Designer đã bắt đầu LÀM LẠI thiết kế cho mục #{orderDetailId} (Đơn hàng #{orderId}).",
                    $"/seller/orders/{orderId}",
                    orderId,
                    orderDetailId,
                    newStatus
                );
            }
            else if (currentStatus == newStatus)
            {
                isStatusChanged = false;
            }
            else
            {
                throw new InvalidOperationException($"Invalid transition from {currentStatus} to {newStatus} for Order Detail {orderDetailId}.");
            }

            // 3. Lưu thay đổi vào Database
            if (isStatusChanged)
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }
        public async Task<bool> AssignDesignerToOrderDetailAsync(int orderDetailId, string designerUserId, string sellerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy OrderDetail và kiểm tra quyền sở hữu của Seller
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Order)
                    .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId && od.Order.SellerUserId == sellerId);
                if (orderDetail == null)
                {
                    // Hoặc không tồn tại, hoặc không thuộc quyền của Seller này
                    return false;
                }

                // 2. Kiểm tra xem Designer có được phép làm việc với Seller này không
                var isAssignmentAllowed = await _context.DesignerSellers
                    .AnyAsync(ds => ds.SellerUserId == sellerId && ds.DesignerUserId == designerUserId);

                if (!isAssignmentAllowed)
                {
                    // Designer này không nằm trong danh sách cho phép
                    throw new InvalidOperationException("Designer không được phân quyền cho Seller này.");
                }

                // 3. Thực hiện gán và cập nhật
                orderDetail.AssignedDesignerUserId = designerUserId;
                orderDetail.AssignedAt = DateTime.UtcNow;

                // 4. Cập nhật trạng thái của Order thành "NEEDDESIGN" (ID = 3)
                // Chỉ cập nhật nếu trạng thái hiện tại là CREATED (ID = 2) hoặc tương tự
                if (orderDetail.Order.StatusOrder == 2) // Giả sử 2 là "Lên Đơn"
                {
                    orderDetail.Order.StatusOrder = 3; // 3 là "Cần Design"
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // Ném lại lỗi để controller xử lý
            }
        }
        /// <summary>
        /// Gán một designer cho toàn bộ các chi tiết của một đơn hàng.
        /// </summary>
        /// <param name="orderId">ID của đơn hàng cần gán.</param>
        /// <param name="designerUserId">ID của designer được gán.</param>
        /// <param name="sellerId">ID của seller đang thực hiện hành động (để kiểm tra quyền).</param>
        /// <returns>Trả về true nếu thành công.</returns>
        public async Task<bool> AssignDesignerToOrderAsync(int orderId, string designerUserId, string sellerId)
        {
            // Bắt đầu một transaction để đảm bảo tất cả các cập nhật thành công hoặc không có gì cả.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy Order và TẤT CẢ OrderDetails liên quan, đồng thời kiểm tra quyền sở hữu của Seller.
                var order = await _context.Orders
                    .Include(o => o.OrderDetails) // Quan trọng: Tải tất cả các chi tiết!
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerUserId == sellerId);

                if (order == null)
                {
                    // Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu của Seller này.
                    return false;
                }

                // 2. Kiểm tra xem Designer có được phép làm việc với Seller này không (logic này giữ nguyên).
                var isAssignmentAllowed = await _context.DesignerSellers
                    .AnyAsync(ds => ds.SellerUserId == sellerId && ds.DesignerUserId == designerUserId);

                if (!isAssignmentAllowed)
                {
                    // Designer này không nằm trong danh sách cho phép của Seller.
                    throw new InvalidOperationException("Designer không được phân quyền cho Seller này.");
                }

                var assignmentTime = DateTime.UtcNow;

                // 3. Lặp qua TẤT CẢ các OrderDetail của đơn hàng và thực hiện gán.
                foreach (var detail in order.OrderDetails)
                {
                    detail.AssignedDesignerUserId = designerUserId;
                    detail.AssignedAt = assignmentTime;
                    // <<< CẬP NHẬT MỚI: Đặt ProductionStatus của OrderDetail thành NEED_DESIGN >>>
                    detail.ProductionStatus = ProductionStatus.NEED_DESIGN;
                }

                // 4. Cập nhật trạng thái của Order chính thành "NEEDDESIGN" (ID = 3).
                // Chỉ cập nhật nếu trạng thái hiện tại phù hợp (ví dụ: CREATED hoặc Lên Đơn).
                if (order.StatusOrder == 1 || order.StatusOrder == 2) // Giả sử 1, 2 là các trạng thái ban đầu
                {
                    order.StatusOrder = 3; // 3 là "Cần Design"
                }
                // ✅ BẮT ĐẦU GỬI THÔNG BÁO
                try
                {
                    // 1. Gửi thông báo (chuông) cho Designer
                    await _notificationService.CreateAndSendNotificationAsync(
                        designerUserId,
                        $"Bạn vừa được giao toàn bộ các mục thiết kế cho Đơn hàng #{orderId}.",
                        $"/designer/tasks"
                    );

                    // 2. Gửi cập nhật real-time
                    var orderGroupName = $"order_{orderId}";
                    await _hubContext.Clients.Group(orderGroupName).SendAsync(
                        "OrderStatusChanged",
                        new { orderId = orderId, newStatus = ProductionStatus.NEED_DESIGN.ToString() }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,"Lỗi khi gửi thông báo SignalR cho AssignDesignerToOrderAsync");
                }
                // ✅ KẾT THÚC GỬI THÔNG BÁO

                // Lưu tất cả các thay đổi vào cơ sở dữ liệu.
                await _context.SaveChangesAsync();

                // Hoàn tất transaction thành công.
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                // Nếu có bất kỳ lỗi nào, hủy bỏ tất cả các thay đổi.
                await transaction.RollbackAsync();
                throw; // Ném lại lỗi để controller xử lý và trả về lỗi 500.
            }
        }
        /// <summary>
        /// Hàm trợ giúp private để gửi thông báo và cập nhật SignalR
        /// </summary>
        private async Task SendNotificationAndRealtimeUpdateAsync(
            string userIdToNotify,
            string message,
            string redirectUrl,
            int orderId,
            int? orderDetailId, // Dùng int? (nullable) để biết khi nào là cập nhật order-level
            ProductionStatus newStatus,
            bool isOrderLevel = false)
        {
            try
            {
                // 1. Gửi thông báo (lưu DB và đẩy qua chuông)
                if (!string.IsNullOrEmpty(userIdToNotify))
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        userIdToNotify,
                        message,
                        redirectUrl
                    );
                }

                // 2. Gửi cập nhật real-time cho ai đang xem trang
                var orderGroupName = $"order_{orderId}";
                object payload;

                if (isOrderLevel || orderDetailId == null)
                {
                    // Cập nhật cho cả Order
                    payload = new { orderId = orderId, newStatus = newStatus.ToString() };
                }
                else
                {
                    // Cập nhật cho một OrderDetail cụ thể
                    payload = new { orderId = orderId, orderDetailId = orderDetailId, newStatus = newStatus.ToString() };
                }
        
                await _hubContext.Clients.Group(orderGroupName).SendAsync(
                    "OrderStatusChanged",
                    payload
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho Order {OrderId}, Detail {OrderDetailId}", orderId, orderDetailId);
                // Không ném lỗi, việc gửi thông báo thất bại không nên làm hỏng transaction chính
            }
        }
        // DesignerTaskService.cs

        /// <summary>
        /// Cập nhật trạng thái thiết kế của một chi tiết đơn hàng (OrderDetail).
        /// </summary>
        /// <param name="orderDetailId">ID chi tiết đơn hàng.</param>
        /// <param name="newStatus">Trạng thái ProductionStatus mới.</param>
        /// <returns>True nếu cập nhật thành công, False nếu thất bại hoặc không tìm thấy.</returns>
        /* public async Task<bool> UpdateStatusAsync(int orderDetailId, ProductionStatus newStatus)
         {
             // 1. Xác thực trạng thái đầu vào (Validation)
             // Tận dụng enum: chỉ kiểm tra nếu trạng thái nằm trong phạm vi design
             var designStatuses = new[]
             {
         ProductionStatus.NEED_DESIGN,
         ProductionStatus.DESIGNING,
         ProductionStatus.CHECK_DESIGN,
         ProductionStatus.DESIGN_REDO
     };

             if (!designStatuses.Contains(newStatus))
             {
                 // Lỗi này sẽ được gửi về client
                 throw new ArgumentException($"Invalid design status: {newStatus}. Must be one of {string.Join(", ", designStatuses)}.");
             }

             // 2. Tìm kiếm chi tiết đơn hàng
             var orderDetail = await _context.OrderDetails
                 .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

             if (orderDetail == null)
             {
                 return false; // Không tìm thấy bản ghi
             }

             // Lấy trạng thái hiện tại (nếu null, giả định là NEED_DESIGN, hoặc trạng thái đầu tiên)
             var currentStatus = orderDetail.ProductionStatus ?? ProductionStatus.NEED_DESIGN;


             // 3. Logic Nghiệp vụ (Business Rules)

             // Logic 1: Accept Design (NEED_DESIGN -> DESIGNING)
             if (currentStatus == ProductionStatus.NEED_DESIGN && newStatus == ProductionStatus.DESIGNING)
             {
                 orderDetail.ProductionStatus = newStatus;
             }
             // Logic 2: Upload/Send to Check (DESIGNING -> CHECK_DESIGN)
             else if (currentStatus == ProductionStatus.DESIGNING && newStatus == ProductionStatus.CHECK_DESIGN)
             {
                 orderDetail.ProductionStatus = newStatus;
                 // 2. <<< THÊM: Cập nhật StatusOrder trong Order (Trạng thái tổng thể) >>>
                 // Giả sử StatusOrder (int) 5 tương ứng với CHECK_DESIGN

             }
             // Logic 3: Start Redo (DESIGN_REDO -> DESIGNING)
             else if (currentStatus == ProductionStatus.DESIGN_REDO && newStatus == ProductionStatus.DESIGNING)
             {
                 orderDetail.ProductionStatus = newStatus;
             }
             // Logic 4: Trạng thái đã đúng
             else if (currentStatus == newStatus)
             {
                 return true;
             }
             else
             {
                 // Trường hợp chuyển trạng thái không hợp lệ theo logic nghiệp vụ
                 throw new InvalidOperationException($"Invalid transition from {currentStatus} to {newStatus} for Order Detail {orderDetailId}.");
             }

             // 4. Lưu thay đổi vào Database
             await _context.SaveChangesAsync();

             return true;
         }*/

        public async Task<DesignTaskDetailDto> GetTaskDetailAsync(int orderDetailId, string designerId)
        {
            // 1. Lấy thông tin Task chính (logic tương tự như GetAssignedTasksAsync nhưng cho 1 item)
            var taskInfo = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductVariant.Product)
                .Where(od => od.OrderDetailId == orderDetailId && od.AssignedDesignerUserId == designerId)
                .Select(od => new DesignTaskDto
                {
                    // SAO CHÉP TẤT CẢ CÁC TRƯỜNG TỪ selector TRONG HÀM GetAssignedTasksAsync
                    OrderDetailId = od.OrderDetailId,
                    OrderId = od.OrderId,
                    OrderCode = od.Order.OrderCode,
                    ProductName = od.ProductVariant.Product.ProductName,
                    ProductDescribe = od.ProductVariant.Product.Describe,
                    ProductTemplate = od.ProductVariant.Product.Template,
                    OrderStatus = od.Order.StatusOrder,
                    Quantity = od.Quantity,
                    LinkImg = od.LinkImg,
                    LinkThankCard = od.LinkThanksCard,
                    LinkFileDesign = od.LinkFileDesign,
                    Note = od.Note,
                    AssignedAt = od.AssignedAt,
                    ProductionStatus = od.ProductionStatus.ToString(),
                    // Chúng ta sẽ lấy 'Reason' từ bảng Logs bên dưới
                    ProductDetails = od.ProductVariant != null ? new ProductDetails
                    {
                        ProductVariantId = od.ProductVariant.ProductVariantId.ToString(),
                        LengthCm = od.ProductVariant.LengthCm,
                        HeightCm = od.ProductVariant.HeightCm,
                        WidthCm = od.ProductVariant.WidthCm,
                        ThicknessMm = od.ProductVariant.ThicknessMm,
                        SizeInch = od.ProductVariant.SizeInch,
                        Layer = od.ProductVariant.Layer,
                        CustomShape = od.ProductVariant.CustomShape,
                        Sku = od.ProductVariant.Sku
                    } : null
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (taskInfo == null)
            {
                // Không tìm thấy task hoặc designer không có quyền
                return null;
            }

            // 2. Lấy danh sách các log bị từ chối (REJECTED hoặc QC_FAIL)
            var relevantEventTypes = new[] { "DESIGN_REJECTED", "QC_FAIL", "DESIGN_APPROVED", };

            var logs = await _context.OrderDetailLogs
                .Include(log => log.ActorUser) // Join với bảng User để lấy tên
                .Where(log => log.OrderDetailId == orderDetailId && relevantEventTypes.Contains(log.EventType))
                .OrderByDescending(log => log.CreatedAt) // Mới nhất lên trước
                .Select(log => new OrderDetailLogDto
                {
                    OrderDetailLogId = log.OrderDetailLogId,
                    EventType = log.EventType,
                    Reason = log.Reason,
                    CreatedAt = log.CreatedAt,
                    // Lấy tên đầy đủ, nếu không có thì lấy UserName
                    UserName = log.ActorUser.FullName ?? log.ActorUser.UserName
                })
                .ToListAsync();

            // 3. Kết hợp lại và trả về
            return new DesignTaskDetailDto
            {
                TaskInfo = taskInfo,
                Logs = logs
            };
        }
        // Lấy các task đã qua bước thiết kế (để hiển thị lịch sử)
        public async Task<PaginatedResult<DesignTaskDto>> GetDesignedHistoryAsync(
         string designerId,
         int? productId, // THÊM MỚI
         string? sellerId,
         string? searchTerm,
         DateTime? startDate,
         DateTime? endDate,
         int page,
         int pageSize)
        {
            // Các trạng thái "Pending" - Sẽ BỊ LOẠI KHỎI history
            var pendingStatuses = new[]
            {
                ProductionStatus.DRAFT,
                ProductionStatus.CREATED,
                ProductionStatus.NEED_DESIGN,
                ProductionStatus.DESIGNING,
                ProductionStatus.DESIGN_REDO
            };

            var queryable = _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductVariant.Product)
                .Where(od => od.AssignedDesignerUserId == designerId &&
                             od.NeedDesign == true &&
                             od.ProductionStatus.HasValue &&
                             !pendingStatuses.Contains(od.ProductionStatus.Value));

            // 1. Áp dụng Filter
            if (productId.HasValue) // THÊM MỚI
            {
                queryable = queryable.Where(od => od.ProductVariant.Product.ProductId == productId.Value);
            }

            if (!string.IsNullOrEmpty(sellerId))
            {
                queryable = queryable.Where(od => od.Order.SellerUserId == sellerId);
            }

            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                queryable = queryable.Where(od => od.AssignedAt >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                queryable = queryable.Where(od => od.AssignedAt <= end);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var term = searchTerm.ToLower().Trim();
                queryable = queryable.Where(od =>
                    od.Order.OrderCode.ToLower().Contains(term) ||
                    od.ProductVariant.Product.ProductName.ToLower().Contains(term)
                );
            }

            // 2. Lấy tổng số lượng (TRƯỚC KHI Phân trang)
            var totalCount = await queryable.CountAsync();

            // 3. Áp dụng Phân trang và Sắp xếp
            var items = await queryable
                .OrderByDescending(od => od.AssignedAt) // Sắp xếp mới nhất lên trước
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(od => new DesignTaskDto
                {
                    OrderDetailId = od.OrderDetailId,
                    OrderId = od.OrderId,
                    OrderCode = od.Order.OrderCode,
                    ProductName = od.ProductVariant.Product.ProductName,
                    ProductDescribe = od.ProductVariant.Product.Describe,
                    ProductTemplate = od.ProductVariant.Product.Template,
                    OrderStatus = od.Order.StatusOrder,
                    Quantity = od.Quantity,
                    LinkImg = od.LinkImg,
                    LinkThankCard = od.LinkThanksCard,
                    LinkFileDesign = od.LinkFileDesign,
                    Note = od.Note,
                    AssignedAt = od.AssignedAt,
                    // LUÔN LUÔN TRẢ VỀ "Completed"
                    ProductionStatus = "Completed",

                    // Vẫn lấy Reason nếu nó đã từng bị REDO
                    Reason = (od.ProductionStatus == ProductionStatus.DESIGN_REDO || od.ProductionStatus == ProductionStatus.QC_FAIL)
                       ? (from log in _context.OrderDetailLogs
                          where log.OrderDetailId == od.OrderDetailId &&
                                (log.EventType == "DESIGN_REJECTED" || log.EventType == "QC_FAIL")
                          orderby log.CreatedAt descending
                          select log.Reason
                         ).FirstOrDefault()
                       : null,

                    ProductDetails = od.ProductVariant != null ? new ProductDetails
                    {
                        ProductVariantId = od.ProductVariant.ProductVariantId.ToString(),
                        LengthCm = od.ProductVariant.LengthCm,
                        HeightCm = od.ProductVariant.HeightCm,
                        WidthCm = od.ProductVariant.WidthCm,
                        ThicknessMm = od.ProductVariant.ThicknessMm,
                        SizeInch = od.ProductVariant.SizeInch,
                        Layer = od.ProductVariant.Layer,
                        CustomShape = od.ProductVariant.CustomShape,
                        Sku = od.ProductVariant.Sku
                    } : null
                })
                .AsNoTracking()
                .ToListAsync();

            return new PaginatedResult<DesignTaskDto>
            {
                Items = items,
                Total = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        public async Task<IEnumerable<ProductFilterDto>> GetDesignerProductsAsync(string designerId)
        {
            var products = await _context.OrderDetails
                // Chỉ lấy các OrderDetail của designer này
                .Where(od => od.AssignedDesignerUserId == designerId && od.NeedDesign == true)
                // Include Product
                .Include(od => od.ProductVariant.Product)
                // Chọn thông tin Product
                .Select(od => od.ProductVariant.Product)
                // Lọc ra các sản phẩm duy nhất (distinct)
                .Distinct()
                // Chỉ lấy ID và Tên
                .Select(p => new ProductFilterDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName
                })
                .AsNoTracking()
                .ToListAsync();

            return products;
        }
        public async Task<IEnumerable<SellerFilterDto>> GetDesignerSellersAsync(string designerId)
        {
            // 1. Lấy ra danh sách các Seller ID duy nhất
            var sellerIds = await _context.OrderDetails
                .Where(od => od.AssignedDesignerUserId == designerId && od.NeedDesign == true)
                .Include(od => od.Order)
                .Select(od => od.Order.SellerUserId)
                .Distinct()
                .ToListAsync();

            // 2. Truy vấn bảng Users (Giả sử là _context.Users) để lấy tên
            //    (Nếu bảng User của bạn tên khác, hãy thay _context.Users)
            var sellers = await _context.Users
                .Where(u => sellerIds.Contains(u.Id))
                .Select(u => new SellerFilterDto
                {
                    SellerId = u.Id,
                    SellerName = u.FullName ?? u.UserName // Hiển thị FullName, fallback về UserName
                })
                .AsNoTracking()
                .ToListAsync();

            return sellers;
        }
    }
}