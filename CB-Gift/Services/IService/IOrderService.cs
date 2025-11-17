using CB_Gift.Models;
using CB_Gift.DTOs;
using CB_Gift.Models.Enums;
using CB_Gift.Data;
using CB_Gift.Orders.Import;

namespace CB_Gift.Services.IService
{
    public interface IOrderService
    {
        // Lọc + phân trang cho Seller
        Task<(List<OrderWithDetailsDto> Orders, int TotalCount)> GetFilteredAndPagedOrdersForSellerAsync(
            string sellerUserId,
            string? status,
            string? searchTerm,
            string? sortColumn,
            string? sortDirection,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize);
        // ✅ Lọc + phân trang cho tất cả (API GetAllOrders)
        Task<(IEnumerable<OrderWithDetailsDto> Orders, int Total)> GetFilteredAndPagedOrdersAsync(
            string? status,
            string? searchTerm,
            string? sortColumn,
            string? sellerId,
            string? sortDirection,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize);
        Task<List<OrderWithDetailsDto>> GetAllOrders();
        Task<List<OrderDto>> GetOrdersForSellerAsync(string sellerUserId);
        Task<List<OrderWithDetailsDto>> GetOrdersAndOrderDetailForSellerAsync(string sellerUserId);
        Task<OrderWithDetailsDto?> GetOrderDetailAsync(int orderId, string sellerUserId);

        Task<EndCustomer> CreateCustomerAsync(EndCustomerCreateRequest request);
        Task<int> CreateOrderAsync(OrderCreateRequest request, string sellerUserId);
        Task AddOrderDetailAsync(int orderId, OrderDetailCreateRequest request, string sellerUserId);

        Task<MakeOrderResponse> MakeOrder(MakeOrderDto request, string sellerUserId);
        Task<MakeOrderResponse> UpdateOrderAsync(int orderId, OrderUpdateDto request, string sellerUserId);

        Task<OrderStatsDto> GetOrderStatsForSellerAsync(string sellerUserId);

        Task<bool> DeleteOrderAsync(int orderId, string sellerUserId);
        Task<bool> SellerApproveOrderDesignAsync(int orderId, ProductionStatus action, string sellerId);
        Task<bool> SellerApproveOrderDetailDesignAsync(int orderDetailId, ProductionStatus action, string sellerId, string? reason);
        Task<bool> SendOrderToReadyProdAsync(int orderId, string sellerId);
        Task<ApproveOrderResult> ApproveOrderForShippingAsync(int orderId);
        Task<(IEnumerable<OrderWithDetailsDto> Orders, int Total)> GetFilteredAndPagedOrdersForInvoiceAsync(
             string? status,
             string? searchTerm,
             string? seller, 
             string? sortColumn,
             string? sortDirection,
             DateTime? fromDate,
             DateTime? toDate,
             int page,
             int pageSize);

        Task<IEnumerable<string>> GetUniqueSellersAsync(string? status);
        /// <summary>
        /// Import đơn hàng từ file Excel.
        /// sellerUserId = id của Seller đang đăng nhập.
        /// </summary>
        Task<OrderImportResult> ImportFromExcelAsync(IFormFile file, string sellerUserId);
    }
}
