using CB_Gift.DTOs;
using CB_Gift.Models;

namespace CB_Gift.Services.IService
{
    public interface IOrderDetailService
    {
        Task<OrderDetail> GetOrderDetailByIdAsync(int orderDetailId);
        Task<OrderDetail?> AcceptOrderDetailAsync(int orderDetailId);
        Task<OrderDetail?> RejectOrderDetailAsync(int orderDetailId, QcRejectRequestDto request, string qcUserId);
    }
}
