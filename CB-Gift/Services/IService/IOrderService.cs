using CB_Gift.Models;

namespace CB_Gift.Services.IService
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllOrders();
    }
}
