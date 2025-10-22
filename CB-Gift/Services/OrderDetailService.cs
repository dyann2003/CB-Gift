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
        private async Task<OrderDetail?> UpdateProductionStatusAsync(int orderDetailId, int newStatus)
        {
            var orderDetail = await _context.OrderDetails.FindAsync(orderDetailId);

            if (orderDetail == null)
            {
                return null;
            }

            orderDetail.ProductionStatus = (ProductionStatus)newStatus;

            await _context.SaveChangesAsync();

            return orderDetail;
        }
    }

}
