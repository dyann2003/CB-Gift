using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class OrderDetailService : IOrderDetailService
    {
        private readonly CBGiftDbContext _context;

        public OrderDetailService(CBGiftDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<OrderDetail?> GetOrderDetailByIdAsync(int orderDetailId)
        {
            return await _context.OrderDetails
                .Include(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);
        }

        public async Task<OrderDetail?> AcceptOrderDetailAsync(int orderDetailId)
        {
            return await UpdateProductionStatusAsync(orderDetailId, 9);
        }

        public async Task<OrderDetail?> RejectOrderDetailAsync(int orderDetailId)
        {
            return await UpdateProductionStatusAsync(orderDetailId, 11);
        }
        /* private async Task<OrderDetail?> UpdateProductionStatusAsync(int orderDetailId, int newStatus)
         {
             var orderDetail = await _context.OrderDetails.FindAsync(orderDetailId);

             if (orderDetail == null)
             {
                 return null;
             }

             orderDetail.ProductionStatus = (ProductionStatus)newStatus;

             await _context.SaveChangesAsync();

             return orderDetail;
         }*/
        private async Task<OrderDetail?> UpdateProductionStatusAsync(int orderDetailId, int newStatus)
        {
            // Bắt đầu một Transaction để đảm bảo tính nhất quán của dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Cập nhật ProductionStatus của OrderDetail hiện tại
                // Phải tải OrderDetail mà không cần Include Order ở đây
                var orderDetail = await _context.OrderDetails.FindAsync(orderDetailId);

                if (orderDetail == null)
                {
                    await transaction.RollbackAsync();
                    return null;
                }

                var newProductionStatus = (ProductionStatus)newStatus;
                orderDetail.ProductionStatus = newProductionStatus;

                // 2. Tải Order cha CÙNG VỚI TẤT CẢ OrderDetails
                var order = await _context.Orders
                    .Include(o => o.OrderDetails) // Quan trọng: Phải Include tất cả OrderDetails
                    .FirstOrDefaultAsync(o => o.OrderId == orderDetail.OrderId);

                if (order == null)
                {
                    await transaction.RollbackAsync();
                    return null;
                }

                // 3. CẬP NHẬT ORDER CHA: Dựa trên trạng thái ProductionStatus thấp nhất
                var allOrderDetails = order.OrderDetails.ToList();

                // Chỉ xử lý nếu có chi tiết đơn hàng
                if (allOrderDetails.Any())
                {
                    // Tìm trạng thái ProductionStatus thấp nhất (giá trị int nhỏ nhất)
                    // Giá trị 1 (CREATED) được sử dụng làm giá trị mặc định an toàn
                    var minProductionStatusValue = allOrderDetails
                        .Min(od => (int)od.ProductionStatus.GetValueOrDefault((ProductionStatus)1));

                    var minProductionStatus = (ProductionStatus)minProductionStatusValue;

                    // Ánh xạ trạng thái thấp nhất này sang StatusOrder
                    var newOrderStatus = MapProductionStatusToOrderStatus(minProductionStatus);

                    // Cập nhật StatusOrder của Order cha
                    order.StatusOrder = newOrderStatus;
                }

                // 4. Lưu thay đổi và Commit Transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return orderDetail;
            }
            catch (Exception)
            {
                // Ghi log lỗi ở đây
                await transaction.RollbackAsync();
                // Ném lại lỗi nếu bạn muốn Controller xử lý
                throw;
            }
        }
        private int MapProductionStatusToOrderStatus(ProductionStatus productionStatus)
        {
            return productionStatus switch
            {
                ProductionStatus.DRAFT => 1,
                ProductionStatus.CREATED => 2,
                ProductionStatus.NEED_DESIGN => 3,
                ProductionStatus.DESIGNING => 4,
                ProductionStatus.CHECK_DESIGN => 5,
                ProductionStatus.DESIGN_REDO => 6,
                ProductionStatus.READY_PROD => 8,
                ProductionStatus.IN_PROD => 9,
                ProductionStatus.FINISHED => 10,
                ProductionStatus.QC_DONE => 11, // Đã Kiểm tra Chất lượng
                ProductionStatus.QC_FAIL => 12, // Lỗi Sản xuất (Cần Rework)
                ProductionStatus.PACKING => 13,
                ProductionStatus.PROD_REWORK => 15,
                ProductionStatus.HOLD => 16,
                ProductionStatus.CANCELLED => 17,
                _ => 1
            };
        }
    }

}
