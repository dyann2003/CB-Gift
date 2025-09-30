using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IOrderService
    {
        Task<List<OrderDto>> GetOrdersForSellerAsync(string sellerUserId);
        Task<int> CreateOrderAsync(OrderCreateRequest request, string sellerUserId);
        Task<OrderWithDetailsDto?> GetOrderDetailAsync(int orderId, string sellerUserId);
        Task AddOrderDetailAsync(int orderId, OrderDetailCreateRequest request, string sellerUserId);
    }
}
