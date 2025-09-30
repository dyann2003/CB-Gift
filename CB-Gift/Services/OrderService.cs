using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class OrderService : IOrderService
    {
        private readonly CBGiftDbContext _context;
        public OrderService(CBGiftDbContext context) 
        {
            _context = context;
        }
        public async Task<List<Order>> GetAllOrders()
        {
           return await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.StatusOrderNavigation)
                .ToListAsync();
        }
    }
}
