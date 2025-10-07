
﻿using CB_Gift.Models;
﻿using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IOrderService
    {
        Task<List<Order>> GetAllOrders();
        Task<List<OrderDto>> GetOrdersForSellerAsync(string sellerUserId);
        Task<List<OrderWithDetailsDto>> GetOrdersAndOrderDetailForSellerAsync(string sellerUserId);
        Task<int> CreateOrderAsync(OrderCreateRequest request, string sellerUserId);
        Task<OrderWithDetailsDto?> GetOrderDetailAsync(int orderId, string sellerUserId);
        Task AddOrderDetailAsync(int orderId, OrderDetailCreateRequest request, string sellerUserId);
        Task<EndCustomer> CreateCustomerAsync(EndCustomerCreateRequest request);
    }
}
