
using CB_Gift.Data;
using AutoMapper;
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
                 .ThenInclude(od => od.ProductVariant)
            .Where(o => o.SellerUserId == sellerUserId)
            .OrderByDescending(o => o.OrderDate)
             .ProjectTo<OrderWithDetailsDto>(_mapper.ConfigurationProvider)
            .ToListAsync();
        }
        public async Task<OrderWithDetailsDto?> GetOrderDetailAsync(int orderId, string sellerUserId)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.OrderId == orderId);

            if (!string.IsNullOrEmpty(sellerUserId))
            {
                query = query.Where(o => o.SellerUserId == sellerUserId);
            }


            // Map trực tiếp sang DTO gồm cả collection Details
            var dto = await query
            .ProjectTo<OrderWithDetailsDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

            return dto;
        }

        public async Task<EndCustomer> CreateCustomerAsync(EndCustomerCreateRequest request)
        {
            var customer = new EndCustomer
            {
                Name = request.Name,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Address1 = request.Address1,
                Zipcode = request.ZipCode,
                ShipState = request.ShipState,
                ShipCity = request.ShipCity,
                ShipCountry = request.ShipCountry
            };
            _context.EndCustomers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;

        }

        public async Task<int> CreateOrderAsync(OrderCreateRequest request, string sellerUserId)
        {
            var order = _mapper.Map<Order>(request);
            order.SellerUserId = sellerUserId;
            order.OrderDate = DateTime.UtcNow;
            order.ProductionStatus = "CREATED";
            order.StatusOrder = 1;
            if (order.ActiveTts == true)
            {
                order.TotalCost += 1; // cộng thêm 1 là giá CostScan TTS
            }
           


            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // không thêm OrdeDetail ở CreateOrder
           /* if (request.OrderDetails?.Any() == true)
            {
                var details = _mapper.Map<List<OrderDetail>>(request.OrderDetails);
                foreach (var d in details)
                {
                    d.OrderId = order.OrderId;
                    d.CreatedDate = DateTime.UtcNow;
                }
                _context.OrderDetails.AddRange(details);
                await _context.SaveChangesAsync();
            }*/


            return order.OrderId;
        }

        public async Task AddOrderDetailAsync(int orderId, OrderDetailCreateRequest request, string sellerUserId)
        {
            var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerUserId == sellerUserId);
            if (order == null) throw new Exception("Order not found or not yours.");

            var price = await CalculatePriceAsync(request.ProductVariantID);

            var detail = _mapper.Map<OrderDetail>(request);
            detail.OrderId = orderId;
            detail.Price = price;
            detail.CreatedDate = DateTime.UtcNow;
            _context.OrderDetails.Add(detail);    
            await _context.SaveChangesAsync();
            // cập nhập tổng tiền của Order ( TotalCost)
            await RecalculateOrderTotalCost(orderId);
        }
        private async Task<decimal> CalculatePriceAsync(int productVariantId)
        {
            var variant = await _context.ProductVariants
                .Where(pv => pv.ProductVariantId == productVariantId)
                .Select(pv => new { pv.BaseCost, pv.ShipCost })
                .FirstOrDefaultAsync();

            if (variant == null)
                throw new Exception("Không tìm thấy ProductVariant.");

            decimal baseCost = variant.BaseCost ?? 0;
            decimal shipCost = variant.ShipCost ?? 0;
            

            return baseCost + shipCost;
        }
        private async Task RecalculateOrderTotalCost(int orderId)
        {
            // lấy toàn bộ detail thuộc order
            var details = await _context.OrderDetails
                .Include(d => d.ProductVariant)
                .Where(d => d.OrderId == orderId)
                .ToListAsync();
            Console.WriteLine(details.ToString()); // debug
            // nếu không có orderDetail nào thì TotalCost =0;
            if (!details.Any())
            {
                var order = await _context.Orders.FindAsync(orderId);
                if(order != null)
                {
                    order.TotalCost = 0;
                    await _context.SaveChangesAsync();
                }
                return;
            }
            //ItemBase = tổng base của các sản phẩm trong order.
            //maxExtra = phí ExtraShip cao nhất của 1 sẩn phẩm trong order đó.
            //baseShip = phí ShipCost cao nhất của 1 sản phẩm trong order đó.
            //totalQuantity = Sum của detail.Quantity
            // If ActiveTTS = true thì cộng thêm CostScan = 1 vào.
            //totalCost = ItemBase + baseShip
            //if(totalQuantity > 1) totalCost += (totalQuantity-1)*maxExtra ( Cộng thêm phí ship phụ trội khi có thêm sản phẩm)
            decimal itemBase = details.Sum(d =>
            {
                var pv = d.ProductVariant;
                decimal baseCost = pv.BaseCost ?? 0;
                return d.Quantity * baseCost;
            });
            decimal baseShip = details.Max(d=>d.ProductVariant.ShipCost ?? 0); // decimal baseShip không cho phép null, mà ShipCost trong Model cho phép null
            decimal maxExtra = details.Max(d => d.ProductVariant.ExtraShipping??0); // thêm dấu ? được!
            decimal totalQty = details.Sum(d => d.Quantity);
            decimal totalCost = itemBase + baseShip;
            if(totalQty > 1) 
            {
                totalCost += (totalQty - 1) * maxExtra;
            }
            // cập nhập lại TotalCost trong order
            var orderToUpdate = await _context.Orders.FindAsync(orderId);
            if(orderToUpdate != null)
            {
                orderToUpdate.TotalCost += totalCost;
                await _context.SaveChangesAsync();
            }
        }
    }
}

