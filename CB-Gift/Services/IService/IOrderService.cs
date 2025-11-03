// Trong IOrderService.cs

using CB_Gift.Models;
using CB_Gift.DTOs;
using CB_Gift.Models.Enums;

namespace CB_Gift.Services.IService
{
    public interface IOrderService
    {
        // ... (Giữ nguyên các phương thức cũ)

        // ✅ PHƯƠNG THỨC MỚI: Chỉ để lấy 4 chỉ số Dashboard
        Task<DashboardStatsDto> GetDashboardStatsForSellerAsync(string sellerUserId);

        // ✅ PHƯƠNG THỨC TỐI ƯU CHO BẢNG DỮ LIỆU
        Task<(List<OrderWithDetailsDto> Orders, int TotalCount)> GetFilteredAndPagedOrdersForSellerAsync(
            string sellerUserId,
            string? status,
            string? searchTerm,
            string? sortColumn,
            string? sortDirection,
            int page,
            int pageSize);

        // ... (Giữ nguyên các phương thức còn lại)
        Task<List<OrderWithDetailsDto>> GetAllOrders();
        Task<List<OrderDto>> GetOrdersForSellerAsync(string sellerUserId);
        Task<List<OrderWithDetailsDto>> GetOrdersAndOrderDetailForSellerAsync(string sellerUserId);
        Task<OrderWithDetailsDto?> GetOrderDetailAsync(int orderId, string sellerUserId);
        Task<EndCustomer> CreateCustomerAsync(EndCustomerCreateRequest request);
        Task<int> CreateOrderAsync(OrderCreateRequest request, string sellerUserId);
        Task AddOrderDetailAsync(int orderId, OrderDetailCreateRequest request, string sellerUserId);
        Task<MakeOrderResponse> MakeOrder(MakeOrderDto request, string sellerUserId);
        Task<MakeOrderResponse> UpdateOrderAsync(int orderId, OrderUpdateDto request, string sellerUserId);
        Task<bool> DeleteOrderAsync(int orderId, string sellerUserId);
        Task<bool> SellerApproveOrderDesignAsync(int orderId, ProductionStatus action, string sellerId);
        Task<bool> SellerApproveOrderDetailDesignAsync(int orderDetailId, ProductionStatus action, string sellerId);
        Task<bool> SendOrderToReadyProdAsync(int orderId, string sellerId);
        Task<ApproveOrderResult> ApproveOrderForShippingAsync(int orderId);
    }
}