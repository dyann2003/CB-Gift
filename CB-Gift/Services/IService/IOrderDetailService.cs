using CB_Gift.Models;

namespace CB_Gift.Services.IService
{
    public interface IOrderDetailService
    {
        Task<OrderDetail> GetOrderDetailByIdAsync(int orderDetailId);
    }
}
