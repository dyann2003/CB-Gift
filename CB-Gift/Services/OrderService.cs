
﻿using CB_Gift.Data;
﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class OrderService : IOrderService
    {
        private readonly CBGiftDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IMapper _mapper;
        public OrderService(CBGiftDbContext context, IMapper mapper, ILogger<OrderService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<List<Order>> GetAllOrders()
        {
           return await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.StatusOrderNavigation)
                .ToListAsync();
        }   
        public async Task<List<OrderDto>> GetOrdersForSellerAsync(string sellerUserId)
        {
            return await _context.Orders
            .Include(o => o.EndCustomer)
            .Include(o => o.StatusOrderNavigation)
            .Include(o => o.OrderDetails)
            .Where(o => o.SellerUserId == sellerUserId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderDto
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                CustomerName = o.EndCustomer.Name,
                TotalCost = o.TotalCost,
                Tracking = o.Tracking,
                StatusOderName = o.StatusOrderNavigation.NameVi
            })
            .ToListAsync();
        }
        public async Task<List<OrderWithDetailsDto>> GetOrdersAndOrderDetailForSellerAsync(string sellerUserId)
        {
            return await _context.Orders
            .Include(o => o.EndCustomer)
            .Include(o => o.StatusOrderNavigation)
            .Include(o => o.OrderDetails)
                 .ThenInclude(od=>od.ProductVariant)
            .Where(o => o.SellerUserId == sellerUserId)
            .OrderByDescending(o => o.OrderDate)
             .ProjectTo<OrderWithDetailsDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
        }

        public async Task<int> CreateOrderAsync(OrderCreateRequest request, string sellerUserId)
        {
            var order = _mapper.Map<Order>(request);
            order.SellerUserId = sellerUserId;
            order.OrderDate = DateTime.UtcNow;
            order.ProductionStatus = "CREATED";
            order.StatusOrder = 1;


            _context.Orders.Add(order);
            await _context.SaveChangesAsync();


            if (request.OrderDetails?.Any() == true)
            {
                var details = _mapper.Map<List<OrderDetail>>(request.OrderDetails);
                foreach (var d in details)
                {
                    d.OrderId = order.OrderId;
                    d.CreatedDate = DateTime.UtcNow;
                }
                _context.OrderDetails.AddRange(details);
                await _context.SaveChangesAsync();
            }


            return order.OrderId;
        }


        public async Task<OrderWithDetailsDto?> GetOrderDetailAsync(int orderId, string sellerUserId)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails)
            .Where(o => o.OrderId == orderId && o.SellerUserId == sellerUserId);
            // Map trực tiếp sang DTO gồm cả collection Details
            var dto = await query
            .ProjectTo<OrderWithDetailsDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

            return dto;
        }


        public async Task AddOrderDetailAsync(int orderId, OrderDetailCreateRequest request, string sellerUserId)
        {
            var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerUserId == sellerUserId);
            if (order == null) throw new Exception("Order not found or not yours.");


            var detail = _mapper.Map<OrderDetail>(request);
            detail.OrderId = orderId;
            detail.CreatedDate = DateTime.UtcNow;


            _context.OrderDetails.Add(detail);
            await _context.SaveChangesAsync();
        }
    }
}

