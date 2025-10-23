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

namespace CB_Gift.Services
{
    public class DesignerTaskService : IDesignerTaskService
    {
        private readonly CBGiftDbContext _context;
        private readonly IImageManagementService _imageService; 

        public DesignerTaskService(CBGiftDbContext context, IImageManagementService imageService) 
        {
            _context = context;
            _imageService = imageService; 
        }

        public async Task<IEnumerable<DesignTaskDto>> GetAssignedTasksAsync(string designerId)
        {
            // Định nghĩa các trạng thái ProductionStatus liên quan đến công việc của designer
            var designStatuses = new[]
            {
                ProductionStatus.NEED_DESIGN,
                ProductionStatus.DESIGNING,
                ProductionStatus.CHECK_DESIGN,
                ProductionStatus.DESIGN_REDO
            };
            // Định nghĩa các trạng thái OrderStatus liên quan đến thiết kế
            var designOrderStatuses = new[] { 3, 4, 5, 6 }; // StatusOrder là kiểu int
            var tasks = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductVariant.Product)
                .Where(od => od.AssignedDesignerUserId == designerId &&
                             od.NeedDesign == true &&
                             // Lọc theo ProductionStatus của OrderDetail
                             od.ProductionStatus.HasValue &&
                             designStatuses.Contains(od.ProductionStatus.Value) &&
                             // <<< TIÊU CHÍ 2: Lọc theo StatusOrder của Order >>>
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
                    // THÊM ProductionStatus vào DTO để designer xem trạng thái chi tiết
                    ProductionStatus = od.ProductionStatus.ToString(),
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
                var orderDetail = await _context.OrderDetails.FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

                if (orderDetail == null || orderDetail.AssignedDesignerUserId != designerId)
                {
                    return false;
                }

                var currentStatus = orderDetail.ProductionStatus ?? ProductionStatus.NEED_DESIGN;

                // Logic nghiệp vụ: Chỉ cho phép upload khi đang ở DESIGNING hoặc DESIGN_REDO
                if (currentStatus != ProductionStatus.DESIGNING && currentStatus != ProductionStatus.DESIGN_REDO)
                {
                    throw new InvalidOperationException($"Cannot upload design file. Current status is {currentStatus}. Must be DESIGNING or DESIGN_REDO.");
                }

                // ********** Logic sau khi đã xác thực trạng thái **********
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
                    // TRƯỜNG HỢP 2: Sử dụng URL của file đã có sẵn trong kho ảnh
                    // (Bạn có thể cần thêm logic xác thực URL này có thuộc về Designer không)
                    finalFileUrl = dto.FileUrl;
                }
                else
                {
                    // Cả hai trường đều trống -> Lỗi
                    throw new ArgumentException("Phải cung cấp File mới hoặc URL File đã có.");
                }

                // 3. Tạo một record OrderDetailDesign mới
                var newDesignRecord = new OrderDetailDesign
                {
                    OrderDetailId = orderDetailId,
                    DesignerUserId = designerId,
                    FileUrl = finalFileUrl,
                    Note = dto.Note,
                    IsFinal = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.OrderDetailDesigns.Add(newDesignRecord);

                // 4. Cập nhật lại LinkFileDesign và ProductionStatus trong OrderDetail
                orderDetail.LinkFileDesign = finalFileUrl;

                // CẬP NHẬT TRẠNG THÁI ORDER DETAIL: DESIGNING/DESIGN_REDO -> CHECK_DESIGN
                // Set giá trị Enum. EF Core Value Converter sẽ tự chuyển nó sang chuỗi (varchar) trong DB.
                orderDetail.ProductionStatus = ProductionStatus.CHECK_DESIGN; 

                // 5. Lưu tất cả thay đổi vào database
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                // Khi xảy ra Exception trong quá trình Upload (bước 2), nó sẽ được bắt ở đây.
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
         //
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
    }
}