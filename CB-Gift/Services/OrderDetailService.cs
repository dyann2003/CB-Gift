using CB_Gift.Data;
using CB_Gift.DTOs;
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
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);
        }

        public async Task<OrderDetail?> AcceptOrderDetailAsync(int orderDetailId)
        {
            return await UpdateProductionStatusAsync(orderDetailId, 9, null, null);
        }

        // ⭐ THAY ĐỔI: Triển khai hàm Reject mới
        public async Task<OrderDetail?> RejectOrderDetailAsync(int orderDetailId, QcRejectRequestDto request, string qcUserId)
        {
            return await UpdateProductionStatusAsync(orderDetailId, 10, qcUserId, request.Reason);
        }

        // ⭐ THAY ĐỔI: Nâng cấp hàm private này
        private async Task<OrderDetail?> UpdateProductionStatusAsync(
            int orderDetailId,
            int newStatus,
            string? actorUserId, // Thêm Actor
            string? reason) // Thêm Reason
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orderDetail = await _context.OrderDetails.FindAsync(orderDetailId);
                if (orderDetail == null)
                {
                    await transaction.RollbackAsync();
                    return null;
                }

                var newProductionStatus = (ProductionStatus)newStatus;
                orderDetail.ProductionStatus = newProductionStatus;

                // ⭐ 1. LOGIC MỚI: GHI LOG NẾU CÓ
                if (!string.IsNullOrEmpty(actorUserId) && !string.IsNullOrEmpty(reason))
                {
                    string eventType = (newProductionStatus == ProductionStatus.PROD_REWORK)
                        ? "QC_REJECTED"
                        : "STATUS_UPDATED";

                    var newLog = new OrderDetailLog
                    {
                        OrderDetailId = orderDetailId,
                        ActorUserId = actorUserId,
                        EventType = eventType,
                        Reason = reason,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.OrderDetailLogs.Add(newLog);
                }

                // 2. Tải Order cha CÙNG VỚI TẤT CẢ OrderDetails
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderDetail.OrderId);

                if (order == null)
                {
                    await transaction.RollbackAsync();
                    return null;
                }

                // 3. CẬP NHẬT ORDER CHA (Logic cũ của bạn)
                var allOrderDetails = order.OrderDetails.ToList();
                if (allOrderDetails.Any())
                {
                    bool hasError = allOrderDetails.Any(od =>
                        od.ProductionStatus == ProductionStatus.QC_FAIL ||
                        od.ProductionStatus == ProductionStatus.PROD_REWORK
                    );
                    if (hasError)
                    {
                        order.StatusOrder = MapProductionStatusToOrderStatus(ProductionStatus.QC_FAIL);
                    }
                    else
                    {
                        var minProductionStatusValue = allOrderDetails
                            .Min(od => (int)od.ProductionStatus.GetValueOrDefault((ProductionStatus)1));

                        var minProductionStatus = (ProductionStatus)minProductionStatusValue;
                        var newOrderStatus = MapProductionStatusToOrderStatus(minProductionStatus);
                        order.StatusOrder = newOrderStatus;
                    }

                }

                // 4. Lưu thay đổi và Commit
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return orderDetail;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
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
