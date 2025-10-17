
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

            var productVariant = await _context.ProductVariants
                                       .AnyAsync(pv => pv.ProductVariantId == request.ProductVariantID);

            if (!productVariant)
            {
                // Sử dụng một loại Exception rõ ràng hơn cho nghiệp vụ (Ví dụ: ArgumentException)
                throw new ArgumentException($"Product Variant with ID {request.ProductVariantID} does not exist.", nameof(request.ProductVariantID));
            }
            if (request.Quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.", nameof(request.Quantity));
            }
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

        public async Task<MakeOrderResponse> MakeOrder(MakeOrderDto request, string sellerUserId)
        {  // Step1: Tạo Endcustomer
            int customerId;
            var newEndCustomer = _mapper.Map<EndCustomer>(request.CustomerInfo);
            _context.EndCustomers.Add(newEndCustomer);
            await _context.SaveChangesAsync();
            customerId = newEndCustomer.CustId;
           //Step2: tạo order
           var order = _mapper.Map<Order>(request.OrderCreate);
            order.SellerUserId = sellerUserId;
            order.EndCustomerId=customerId;
            order.OrderDate = DateTime.Now;
            order.ProductionStatus = "Created";
            order.StatusOrder = 1;
            // Nếu ActiveTTS = true thì cộng thêm CostScan vào tổng
            decimal totalCost = 0;
            if (order.ActiveTts ==true)
            {
                totalCost += 1;
            }
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            // Step 3: Thêm các OrderDetail
            var details = new List<OrderDetail>();

            foreach (var item in request.OrderDetails)
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.ProductVariantId == item.ProductVariantID);

                if (variant == null) throw new Exception("ProductVariant not found.");

                decimal price = (variant.BaseCost ?? 0) + (variant.ShipCost ?? 0);
                totalCost += price * item.Quantity;

                var detail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductVariantId = item.ProductVariantID,
                    Quantity = item.Quantity,
                    LinkImg = item.LinkImg,
                    NeedDesign = item.NeedDesign,
                    LinkThanksCard = item.LinkThanksCard,
                    LinkFileDesign = item.LinkDesign,
                    Accessory = item.Accessory,
                    Note = item.Note,
                    Price = price,
                    CreatedDate = DateTime.UtcNow
                };
                details.Add(detail);
            }

            _context.OrderDetails.AddRange(details);
            order.TotalCost = totalCost;
            await _context.SaveChangesAsync();

            // Step 4: Chuẩn bị dữ liệu trả về
            var response = new MakeOrderResponse
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode ?? null,
                TotalCost = totalCost,
                CustomerName = request.CustomerInfo.Name,
                Details = details.Select(d => new MakeOrderDetailResponse
                {
                    ProductVariantID = d.ProductVariantId,
                    Quantity = d.Quantity,
                    Price = d.Price ?? 0
                }).ToList()
            };

            return response;
        }
        public async Task<MakeOrderResponse> UpdateOrderAsync(int orderId, OrderUpdateDto request, string sellerUserId)
        {
            // Step 1: Tìm đơn hàng và kiểm tra điều kiện
            var order = await _context.Orders
                .Include(o => o.OrderDetails) // Lấy danh sách chi tiết hiện có
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerUserId == sellerUserId);

            if (order == null)
            {
                throw new KeyNotFoundException("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.");
            }

            if (order.StatusOrder != 1)
            {
                throw new InvalidOperationException("Chỉ có thể cập nhật đơn hàng ở trạng thái 'Mới tạo'.");
            }

            // Step 2: Cập nhật thông tin khách hàng và đơn hàng chính
            var customer = await _context.EndCustomers.FindAsync(order.EndCustomerId);
            if (customer != null) _mapper.Map(request.CustomerInfo, customer);
            _mapper.Map(request.OrderUpdate, order);

            // Step 3: Đồng bộ hóa Order Details
            var detailsInRequest = request.OrderDetailsUpdate ?? new List<OrderDetailUpdateRequest>();
            var requestDetailIds = detailsInRequest.Select(d => d.OrderDetailID).ToHashSet();

            // 3.1: Xác định các details cần xóa
            var detailsToRemove = order.OrderDetails
                .Where(d => !requestDetailIds.Contains(d.OrderDetailId))
                .ToList();
            if (detailsToRemove.Any())
            {
                _context.OrderDetails.RemoveRange(detailsToRemove);
            }

            // 3.2: Cập nhật hoặc Thêm mới
            foreach (var detailDto in detailsInRequest)
            {
                if (detailDto.OrderDetailID > 0) // Đây là item cần UPDATE
                {
                    var existingDetail = order.OrderDetails
                        .FirstOrDefault(d => d.OrderDetailId == detailDto.OrderDetailID);
                    if (existingDetail != null)
                    {
                        // Dùng AutoMapper để cập nhật các thuộc tính
                        _mapper.Map(detailDto, existingDetail);
                    }
                }
                else // Đây là item mới cần ADD
                {
                    var newDetail = _mapper.Map<OrderDetail>(detailDto);
                    // Gán các giá trị cần thiết mà không có trong DTO
                    newDetail.OrderId = order.OrderId;
                    newDetail.CreatedDate = DateTime.UtcNow;
                    order.OrderDetails.Add(newDetail); // Thêm vào danh sách của order
                }
            }

            // Step 4: Tính toán lại tổng tiền và lưu thay đổi
            // Bạn nên gọi lại hàm RecalculateOrderTotalCost để đảm bảo logic tính toán là nhất quán
            await _context.SaveChangesAsync(); // Lưu các thay đổi (add, update, remove)

            // Gọi hàm tính toán lại sau khi đã lưu DB để nó lấy được dữ liệu mới nhất
            await RecalculateOrderTotalCost(order.OrderId);

            // Step 5: Chuẩn bị dữ liệu trả về (tương tự như trước)
            var updatedOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            return _mapper.Map<MakeOrderResponse>(updatedOrder);
        }
        public async Task<bool> DeleteOrderAsync(int orderId, string sellerUserId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails) // Lấy cả các order details liên quan
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerUserId == sellerUserId);

            if (order == null)
            {
                return false; // Trả về false để controller xử lý NotFound
            }

            if (order.StatusOrder != 1)
            {
                // Ném ra lỗi để Controller có thể bắt và trả về BadRequest
                throw new InvalidOperationException("Chỉ có thể xóa đơn hàng ở trạng thái 'Daft(Nháp)'.");
            }

            // Xóa các OrderDetail liên quan trước
            if (order.OrderDetails.Any())
            {
                _context.OrderDetails.RemoveRange(order.OrderDetails);
            }

            // Sau đó xóa Order
            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

