using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService; 
using Microsoft.EntityFrameworkCore;
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
            var tasks = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductVariant.Product)
                .Where(od => od.AssignedDesignerUserId == designerId &&
                             od.NeedDesign == true)
                            // (od.Order.StatusOrder == 3 || od.Order.StatusOrder == 6)) // Trạng thái NEEDDESIGN hoặc DESIGN_REDO
                .Select(od => new DesignTaskDto {
                    OrderDetailId = od.OrderDetailId,
                    OrderId = od.OrderId,
                    OrderCode = od.Order.OrderCode,
                    ProductName = od.ProductVariant.Product.ProductName,
                    Quantity = od.Quantity,
                    LinkImg  = od.ProductVariant.Product.ItemLink,
                    Note  = od.Note,
                    AssignedAt = od.AssignedAt
                    })
                .AsNoTracking()
                .ToListAsync();
            return tasks;
        }

        // === PHƯƠNG THỨC UPLOAD ĐÃ ĐƯỢC NÂNG CẤP ===
        public async Task<bool> UploadDesignFileAsync(int orderDetailId, string designerId, UploadDesignDto dto)
        {
            // Bắt đầu một transaction để đảm bảo tất cả các bước đều thành công hoặc thất bại cùng nhau
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Kiểm tra quyền và sự tồn tại của OrderDetail
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Order) // Quan trọng: phải include Order để cập nhật status
                    .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

                // Nếu không tìm thấy, hoặc designer không được gán, hoặc đơn hàng không ở trạng thái cần design -> từ chối
              /*  if (orderDetail == null || orderDetail.AssignedDesignerUserId != designerId || (orderDetail.Order.StatusOrder != 3 && orderDetail.Order.StatusOrder != 6))
                {
                    return false;
                }*/

                // 2. Sử dụng ImageManagementService để upload file lên Cloudinary
                await using var stream = dto.DesignFile.OpenReadStream();
                var uploadedImage = await _imageService.UploadImageForUserAsync(stream, dto.DesignFile.FileName, designerId);

                // 3. Tạo một record OrderDetailDesign mới
                var newDesignRecord = new OrderDetailDesign
                {
                    OrderDetailId = orderDetailId,
                    DesignerUserId = designerId,
                    FileUrl = uploadedImage.SecureUrl, // Lấy URL từ kết quả upload
                    Note = dto.Note,
                    IsFinal = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.OrderDetailDesigns.Add(newDesignRecord);

                // 4. Cập nhật lại LinkFileDesign trong OrderDetail
                orderDetail.LinkFileDesign = uploadedImage.SecureUrl;

                // 5. Cập nhật trạng thái của Order thành "CHECKDESIGN" (ID = 5)
                orderDetail.Order.StatusOrder = 5;

                // 6. Lưu tất cả thay đổi vào database
                await _context.SaveChangesAsync();

                // 7. Nếu mọi thứ thành công, commit transaction
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                // Nếu có bất kỳ lỗi nào xảy ra, rollback transaction
                await transaction.RollbackAsync();
                // Ném lại lỗi để controller có thể xử lý và log lại
                throw;
            }
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
    }
}