using CB_Gift.Models;
using CB_Gift.DTOs;
using CB_Gift.Models.Enums;

namespace CB_Gift.Services.IService
{
    public interface IOrderService
    {
        // ----------------------------------------------------------------------
        // ✅ THÊM PHƯƠNG THỨC MỚI HỖ TRỢ PHÂN TRANG (Mục đích tối ưu)
        // ----------------------------------------------------------------------
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

        // ----------------------------------------------------------------------
        // Giữ nguyên các phương thức khác
        // ----------------------------------------------------------------------
        Task<List<OrderWithDetailsDto>> GetAllOrders();
        // Get Order theo SellerId
        Task<List<OrderDto>> GetOrdersForSellerAsync(string sellerUserId);
        //Get Order and OrderDetail theo SellerId
        Task<List<OrderWithDetailsDto>> GetOrdersAndOrderDetailForSellerAsync(string sellerUserId);
        //Get OrderDetail theo OrderId va SellerId
        Task<OrderWithDetailsDto?> GetOrderDetailAsync(int orderId, string sellerUserId);
        //Step1: Create EndCustomer
        Task<EndCustomer> CreateCustomerAsync(EndCustomerCreateRequest request);
        //Step2: Create Order 
        Task<int> CreateOrderAsync(OrderCreateRequest request, string sellerUserId);
        //Step3: Add OrderDetail vao OrderId
        Task AddOrderDetailAsync(int orderId, OrderDetailCreateRequest request, string sellerUserId);

        //Create Order: Gửi tổng Json: Customer,Order,OrderDetail.
        Task<MakeOrderResponse> MakeOrder(MakeOrderDto request, string sellerUserId);
        Task<MakeOrderResponse> UpdateOrderAsync(int orderId, OrderUpdateDto request, string sellerUserId);

        Task<OrderStatsDto> GetOrderStatsForSellerAsync(string sellerUserId);

        Task<bool> DeleteOrderAsync(int orderId, string sellerUserId);
        Task<bool> SellerApproveOrderDesignAsync(int orderId, ProductionStatus action, string sellerId); // chuyển status order
        Task<bool> SellerApproveOrderDetailDesignAsync(int orderDetailId, ProductionStatus action, string sellerId); // chuyển status orderDetail
        Task<bool> SendOrderToReadyProdAsync(int orderId, string sellerId);// bắn thẳng sang staff, không qua designer
        Task<ApproveOrderResult> ApproveOrderForShippingAsync(int orderId);
    }
}