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

        public async Task<object?> GetOrderDetailByIdAsync(int orderDetailId)
        {
            // 1. Lấy thông tin chi tiết
            var detail = await _context.OrderDetails
                .Include(od => od.ProductVariant).ThenInclude(pv => pv.Product)
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (detail == null) return null;

            // 2. Tính toán TotalItems và ItemIndex (Dựa trên tổng Quantity)
            var siblings = await _context.OrderDetails
                .Where(od => od.OrderId == detail.OrderId)
                .OrderBy(od => od.OrderDetailId) // Sắp xếp cố định để thứ tự không bị nhảy
                .Select(od => new { od.OrderDetailId, od.Quantity })
                .ToListAsync();

            // Tổng số lượng sản phẩm (Ví dụ: 3IT)
            int totalItems = siblings.Sum(s => s.Quantity);

            // Tính vị trí bắt đầu của item này
            int itemIndex = 1;
            foreach (var sib in siblings)
            {
                if (sib.OrderDetailId == orderDetailId) break; // Đã tìm thấy item hiện tại, dừng lại
                itemIndex += sib.Quantity; // Cộng dồn số lượng của các item đứng trước
            }

            // 3. Trả về Anonymous Object
            return new
            {
                // Copy các thuộc tính gốc từ detail
                detail.OrderDetailId,
                detail.OrderId,
                detail.Quantity,
                detail.Price,
                detail.CreatedDate,
                detail.ProductionStatus,
                detail.NeedDesign,
                detail.AssignedAt,
                detail.AssignedDesignerUserId,
                detail.Note,
                detail.LinkImg,
                detail.LinkFileDesign,
                detail.Accessory,

                Order = new
                {
                    detail.Order.OrderCode,
                    Tracking = detail.Order.Tracking
                },

                ProductVariant = new
                {
                    detail.ProductVariant.ProductVariantId,
                    detail.ProductVariant.Sku,
                    detail.ProductVariant.SizeInch,
                    detail.ProductVariant.ThicknessMm,
                    detail.ProductVariant.Layer,
                    detail.ProductVariant.CustomShape,
                    detail.ProductVariant.LengthCm,
                    detail.ProductVariant.HeightCm,
                    detail.ProductVariant.WidthCm,
                    detail.ProductVariant.WeightGram,
                    detail.ProductVariant.BaseCost,
                    detail.ProductVariant.ShipCost,
                    detail.ProductVariant.ExtraShipping,
                    detail.ProductVariant.TotalCost,
                    Product = new
                    {
                        detail.ProductVariant.Product.ProductName,
                        detail.ProductVariant.Product.ProductCode,
                        detail.ProductVariant.Product.CategoryId,
                        detail.ProductVariant.Product.Status,
                        detail.ProductVariant.Product.Describe
                    }
                },

                TotalItems = totalItems,
                ItemIndex = itemIndex
            };
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
                    string eventType = (newProductionStatus == ProductionStatus.PROD_REWORK || newProductionStatus == ProductionStatus.QC_FAIL)
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
                ProductionStatus.SHIPPING => 13,
                ProductionStatus.SHIPPED => 14,
                ProductionStatus.PROD_REWORK => 15,
                ProductionStatus.HOLD => 16,
                ProductionStatus.HOLD_RP => 17,
                ProductionStatus.REFUND => 18,
                _ => 1
            };
        }
    }

}
