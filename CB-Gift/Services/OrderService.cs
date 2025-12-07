using AutoMapper;
using AutoMapper.QueryableExtensions;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Orders.Import;
using CB_Gift.Services.IService;
using ClosedXML.Excel;
using CloudinaryDotNet.Core;
using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions; // Cần thiết cho các thao tác LINQ

namespace CB_Gift.Services
{
    public class OrderService : IOrderService
    {
        private readonly CBGiftDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IShippingService _shippingService;
        private readonly OrderFactory _orderFactory;
        private readonly IValidator<OrderImportRowDto> _validator;
        private readonly ReferenceDataCache _cache;
        public OrderService(CBGiftDbContext context, IMapper mapper, ILogger<OrderService> logger,
            INotificationService notificationService, IHubContext<NotificationHub> hubContext, OrderFactory orderFactory,
            IValidator<OrderImportRowDto> validator, ReferenceDataCache cache, IShippingService shippingService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService;
            _hubContext = hubContext;
            _shippingService = shippingService;
            _orderFactory = orderFactory;
            _validator = validator;
            _cache = cache;
        }



        // ----------------------------------------------------------------------
        // ✅ TRIỂN KHAI PHƯƠNG THỨC HỖ TRỢ PHÂN TRANG (ĐÃ SỬA LỖI biên dịch)
        // ----------------------------------------------------------------------
        public async Task<(List<OrderWithDetailsDto> Orders, int TotalCount)> GetFilteredAndPagedOrdersForSellerAsync(
            string sellerUserId,
            string? status,
            string? searchTerm,
            string? sortColumn,
            string? sortDirection,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            var query = _context.Orders
                .Include(o => o.EndCustomer)
                .Include(o => o.StatusOrderNavigation)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                .Where(o => o.SellerUserId == sellerUserId)
                .AsQueryable();

            // 1. Lọc theo Status
            if (!string.IsNullOrEmpty(status))
                // ✅ Logic MỚI: Cho phép lọc theo NameVi HOẶC Code
                query = query.Where(o =>
                    o.StatusOrderNavigation.NameVi == status ||
                    o.StatusOrderNavigation.Code == status
                );

            // 2. Xử lý Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string term = searchTerm.ToLower();
                query = query.Where(o =>
                    // Tìm kiếm theo OrderCode
                    (o.OrderCode != null && o.OrderCode.ToLower().Contains(term)) ||
                    // Tìm kiếm theo Customer Name
                    (o.EndCustomer != null && o.EndCustomer.Name.ToLower().Contains(term))
                // ❌ Loại bỏ phần tìm kiếm theo Product Variant Name để tránh lỗi biên dịch:
                // o.OrderDetails.Any(od => od.ProductVariant != null && od.ProductVariant.VariantName.ToLower().Contains(term))
                );
            }

            if (fromDate.HasValue)
                query = query.Where(o => o.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.OrderDate < toDate.Value);


            // 3. Đếm tổng số lượng (sau khi lọc, trước khi phân trang)
            var totalCount = await query.CountAsync();

            // 4. Xử lý Sắp xếp
            // Mặc định là OrderDate giảm dần nếu không có cột sắp xếp
            if (!string.IsNullOrEmpty(sortColumn))
            {
                query = sortColumn.ToLower() switch
                {
                    "orderid" => (sortDirection.ToLower() == "asc" ? query.OrderBy(o => o.OrderCode) : query.OrderByDescending(o => o.OrderCode)),
                    "orderdate" => (sortDirection.ToLower() == "asc" ? query.OrderBy(o => o.OrderDate) : query.OrderByDescending(o => o.OrderDate)),
                    "customername" => (sortDirection.ToLower() == "asc" ? query.OrderBy(o => o.EndCustomer.Name) : query.OrderByDescending(o => o.EndCustomer.Name)),
                    "totalamount" => (sortDirection.ToLower() == "asc" ? query.OrderBy(o => o.TotalCost) : query.OrderByDescending(o => o.TotalCost)),
                    _ => query.OrderByDescending(o => o.CreationDate ?? o.OrderDate) // Dùng CreationDate hoặc OrderDate nếu không khớp
                };
            }
            else
            {
                query = query.OrderByDescending(o => o.OrderDate);
            }

            // 5. Phân trang
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                // 6. Project To DTO
                .ProjectTo<OrderWithDetailsDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return (orders, totalCount);
        }

        // ----------------------------------------------------------------------
        // Giữ nguyên các phương thức khác
        // ----------------------------------------------------------------------

        public async Task<List<OrderWithDetailsDto>> GetAllOrders()
        {
            return await _context.Orders
           .Include(o => o.EndCustomer)
           .Include(o => o.StatusOrderNavigation)
           .Include(o => o.OrderDetails)
                 .ThenInclude(od => od.ProductVariant)
           .ProjectTo<OrderWithDetailsDto>(_mapper.ConfigurationProvider)
           .ToListAsync();
        }

        // ❌ Phương thức này không còn cần thiết, GetMyOrders mới sẽ dùng GetFilteredAndPagedOrdersForSellerAsync
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

        // ❌ Phương thức này không còn cần thiết, GetMyOrders mới sẽ dùng GetFilteredAndPagedOrdersForSellerAsync
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
                .Include(o => o.EndCustomer) // Thêm Include để lấy thông tin khách hàng đầy đủ
                .Include(o => o.StatusOrderNavigation)
                 .Include(o => o.OrderDetails) // Lấy details
                    .ThenInclude(od => od.ProductVariant) // Lấy ProductVariant
                .Where(o => o.OrderId == orderId);

            if (!string.IsNullOrEmpty(sellerUserId))
            {
                query = query.Where(o => o.SellerUserId == sellerUserId);
            }


            // Map trực tiếp sang DTO gồm cả collection Details
            var dto = await query
            .ProjectTo<OrderWithDetailsDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();
            // Nếu không tìm thấy đơn (hoặc không thuộc về seller này), trả về null luôn
            if (dto == null) return null;

            // 3. 👇 BỔ SUNG: Truy vấn thủ công bảng Refunds và CancellationRequests
            // (Copy logic từ GetManagerOrderDetailAsync sang)

            var latestRefund = await _context.Refunds
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();


            // 4. 👇 BỔ SUNG: Điền dữ liệu vào DTO

            // --- Ưu tiên 1: Xử lý Hoàn tiền (Refund) ---
            if (latestRefund != null)
            {
                dto.LatestRefundId = latestRefund.RefundId;
                dto.IsRefundPending = (latestRefund.Status == "Pending");
                dto.RefundAmount = latestRefund.Amount;

                // Lấy lý do và bằng chứng
                dto.Reason = latestRefund.Reason; // Lý do Seller gửi
                dto.RejectionReason = latestRefund.StaffRejectionReason; // Lý do Staff từ chối
                dto.ProofUrl = latestRefund.ProofUrl; // Link bằng chứng
            }
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
                Zipcode = "",
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
                if (order != null)
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
            decimal baseShip = details.Max(d => d.ProductVariant.ShipCost ?? 0); // decimal baseShip không cho phép null, mà ShipCost trong Model cho phép null
            decimal maxExtra = details.Max(d => d.ProductVariant.ExtraShipping ?? 0); // thêm dấu ? được!
            decimal totalQty = details.Sum(d => d.Quantity);
            decimal totalCost = itemBase + baseShip;
            if (totalQty > 1)
            {
                totalCost += (totalQty - 1) * maxExtra;
            }
            // cập nhập lại TotalCost trong order
            var orderToUpdate = await _context.Orders.FindAsync(orderId);
            if (orderToUpdate != null)
            {
                orderToUpdate.TotalCost += totalCost;
                await _context.SaveChangesAsync();
            }
        }

        //public async Task<MakeOrderResponse> MakeOrder(MakeOrderDto request, string sellerUserId)
        //{ // Step1: Tạo Endcustomer
        //    int customerId;
        //    var newEndCustomer = _mapper.Map<EndCustomer>(request.CustomerInfo);
        //    _context.EndCustomers.Add(newEndCustomer);
        //    await _context.SaveChangesAsync();
        //    customerId = newEndCustomer.CustId;
        //    //Step2: tạo order
        //    var order = _mapper.Map<Order>(request.OrderCreate);
        //    order.SellerUserId = sellerUserId;
        //    order.EndCustomerId = customerId;
        //    //order.OrderDate = DateTime.Now; // ẩn đi vì khi tạo chỉ là draft thi lưu vào creation date rồi
        //    order.ProductionStatus = "Created";
        //    order.StatusOrder = 1;
        //    // Nếu ActiveTTS = true thì cộng thêm CostScan vào tổng
        //    decimal totalCost = 0;
        //    if (order.ActiveTts == true)
        //    {
        //        totalCost += 1;
        //    }
        //    _context.Orders.Add(order);
        //    await _context.SaveChangesAsync();
        //    // Step 3: Thêm các OrderDetail
        //    var details = new List<OrderDetail>();

        //    foreach (var item in request.OrderDetails)
        //    {
        //        var variant = await _context.ProductVariants
        //            .FirstOrDefaultAsync(v => v.ProductVariantId == item.ProductVariantID);

        //        if (variant == null) throw new Exception("ProductVariant not found.");

        //        decimal price = item.Price ?? 0;     // Dùng đúng giá FE gửi
        //        totalCost += price * item.Quantity;  // FE đã tính unitPrice chuẩn


        //        var detail = new OrderDetail
        //        {
        //            OrderId = order.OrderId,
        //            ProductVariantId = item.ProductVariantID,
        //            Quantity = item.Quantity,
        //            LinkImg = item.LinkImg,
        //            NeedDesign = item.NeedDesign,
        //            LinkThanksCard = item.LinkThanksCard,
        //            LinkFileDesign = item.LinkDesign,
        //            Accessory = item.Accessory,
        //            Note = item.Note,
        //            Price = price,
        //            CreatedDate = DateTime.UtcNow,
        //            ProductionStatus = ProductionStatus.DRAFT
        //        };
        //        details.Add(detail);
        //    }

        //    _context.OrderDetails.AddRange(details);
        //    order.TotalCost = totalCost;
        //    await _context.SaveChangesAsync();

        //    // Step 4: Chuẩn bị dữ liệu trả về
        //    var response = new MakeOrderResponse
        //    {
        //        OrderId = order.OrderId,
        //        OrderCode = order.OrderCode ?? null,
        //        TotalCost = totalCost,
        //        CustomerName = request.CustomerInfo.Name,
        //        Details = details.Select(d => new MakeOrderDetailResponse
        //        {
        //            ProductVariantID = d.ProductVariantId,
        //            Quantity = d.Quantity,
        //            Price = d.Price ?? 0
        //        }).ToList()
        //    };

        //    return response;
        //}

        //public async Task<MakeOrderResponse> MakeOrder(MakeOrderDto request, string sellerUserId)
        //{
        //    // Step 1: Tạo Endcustomer
        //    var newEndCustomer = _mapper.Map<EndCustomer>(request.CustomerInfo);
        //    _context.EndCustomers.Add(newEndCustomer);
        //    await _context.SaveChangesAsync();
        //    int customerId = newEndCustomer.CustId;

        //    // Step 2: Tạo Order
        //    var order = _mapper.Map<Order>(request.OrderCreate);
        //    order.SellerUserId = sellerUserId;
        //    order.EndCustomerId = customerId;
        //    order.ProductionStatus = "Created";
        //    order.StatusOrder = 1;

        //    // 💥 Lấy đúng totalCost từ FE (FE đã tính chuẩn)
        //    decimal totalCost = request.OrderCreate.TotalCost ?? 0;

        //    _context.Orders.Add(order);
        //    await _context.SaveChangesAsync();

        //    // Step 3: Thêm OrderDetail
        //    var details = new List<OrderDetail>();

        //    foreach (var item in request.OrderDetails)
        //    {
        //        // Không cần lấy variant, không cần tính baseCost/shipCost
        //        // FE đã gửi chính xác price rồi

        //        var detail = new OrderDetail
        //        {
        //            OrderId = order.OrderId,
        //            ProductVariantId = item.ProductVariantID,
        //            Quantity = item.Quantity,
        //            LinkImg = item.LinkImg,
        //            NeedDesign = item.NeedDesign,
        //            LinkThanksCard = item.LinkThanksCard,
        //            LinkFileDesign = item.LinkDesign,
        //            Accessory = item.Accessory,
        //            Note = item.Note,
        //            Price = item.Price, // 💥 đúng giá FE gửi
        //            CreatedDate = DateTime.UtcNow,
        //            ProductionStatus = ProductionStatus.DRAFT
        //        };

        //        details.Add(detail);
        //    }

        //    _context.OrderDetails.AddRange(details);

        //    // Lưu totalCost từ FE
        //    order.TotalCost = totalCost;

        //    await _context.SaveChangesAsync();

        //    // Step 4: Response
        //    var response = new MakeOrderResponse
        //    {
        //        OrderId = order.OrderId,
        //        OrderCode = order.OrderCode,
        //        TotalCost = totalCost,
        //        CustomerName = request.CustomerInfo.Name,
        //        Details = details.Select(d => new MakeOrderDetailResponse
        //        {
        //            ProductVariantID = d.ProductVariantId,
        //            Quantity = d.Quantity,
        //            Price = d.Price ?? 0
        //        }).ToList()
        //    };

        //    return response;
        //}

        public async Task<MakeOrderResponse> MakeOrder(MakeOrderDto request, string sellerUserId)
        {
            // Step 1: Tạo Endcustomer
            var newEndCustomer = _mapper.Map<EndCustomer>(request.CustomerInfo);
            _context.EndCustomers.Add(newEndCustomer);
            await _context.SaveChangesAsync();
            int customerId = newEndCustomer.CustId;

            // Step 2: Tạo Order
            var order = _mapper.Map<Order>(request.OrderCreate);
            order.SellerUserId = sellerUserId;
            order.EndCustomerId = customerId;
            order.ProductionStatus = "Created";
            order.StatusOrder = 1;

            // FE gửi totalCost đã tính đúng → GIỮ NGUYÊN
            decimal totalCost = request.OrderCreate.TotalCost ?? 0;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Step 3: Thêm OrderDetail
            var details = new List<OrderDetail>();

            foreach (var item in request.OrderDetails)
            {
                // 💥 Lấy baseCost của variant từ DB
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.ProductVariantId == item.ProductVariantID);

                if (variant == null)
                    throw new Exception("ProductVariant not found.");

                decimal baseCost = variant.BaseCost ?? 0; // ✔ BaseCost chuẩn

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

                    // 💥 Insert PRICE = BASE COST (không dùng FE gửi)
                    Price = baseCost,

                    CreatedDate = DateTime.UtcNow,
                    ProductionStatus = ProductionStatus.DRAFT
                };

                details.Add(detail);
            }

            _context.OrderDetails.AddRange(details);

            // Lưu totalCost từ FE
            order.TotalCost = totalCost;
            await _context.SaveChangesAsync();

            // Step 4: Response
            var response = new MakeOrderResponse
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                TotalCost = totalCost,
                CustomerName = request.CustomerInfo.Name,
                Details = details.Select(d => new MakeOrderDetailResponse
                {
                    ProductVariantID = d.ProductVariantId,
                    Quantity = d.Quantity,

                    // 💥 Response trả đúng BASE COST
                    Price = d.Price ?? 0
                }).ToList()
            };

            return response;
        }


        public async Task<MakeOrderResponse> UpdateOrderAsync(int orderId, OrderUpdateDto request, string sellerUserId)
        {
            // Step 1: Tìm đơn hàng và kiểm tra điều kiện (GIỮ NGUYÊN)
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

            // Map các trường chung
            _mapper.Map(request.OrderUpdate, order);

            // 🔥 FIX QUYẾT ĐỊNH: Đảm bảo TotalCost từ request được ưu tiên và ghi đè
            // Nếu FE gửi totalCost=254000, nó sẽ được gán.
            // Nếu FE gửi totalCost=null, nó sẽ giữ giá trị hiện tại (đã được map từ DB)
            order.TotalCost = request.OrderUpdate.TotalCost ?? order.TotalCost;


            // Step 3: Đồng bộ hóa Order Details (GIỮ NGUYÊN LOGIC)
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

            // Step 4: Chỉ gọi SaveChangesAsync() MỘT LẦN để lưu tất cả các thay đổi.
            await _context.SaveChangesAsync();

            // Step 5: Chuẩn bị dữ liệu trả về (GIỮ NGUYÊN)
            var updatedOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            return _mapper.Map<MakeOrderResponse>(updatedOrder);
        }

        public async Task<OrderDto> UpdateOrderAddressAsync(int orderId, UpdateAddressRequest request, string sellerUserId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerUserId == sellerUserId);

            if (order == null)
                throw new KeyNotFoundException("Không tìm thấy đơn hàng hoặc không có quyền.");

            var customer = await _context.EndCustomers.FindAsync(order.EndCustomerId);

            if (customer == null)
                throw new KeyNotFoundException("Không tìm thấy thông tin khách hàng.");

            // ==========================================================
            // BƯỚC 1: Cập nhật thông tin khách hàng (EndCustomer)
            // ==========================================================

            // Cập nhật các trường tên và địa chỉ chi tiết (EndCustomer)
            customer.Name = request.Name;
            customer.Phone = request.Phone;
            customer.Email = request.Email;
            customer.Address = request.Address;
            customer.Address1 = request.Address1;
            // customer.Zipcode = request.ZipCode; // Nếu ZipCode nằm trong EndCustomer

            // Mapping các TÊN địa lý (Province/District/Ward Name) vào EndCustomer
            // (Giữ lại để đảm bảo không mất dữ liệu hiển thị nếu EndCustomer dùng các trường này)
            customer.ShipState = request.ProvinceName;
            customer.ShipCity = request.DistrictName;
            customer.ShipCountry = request.WardName;

            // ==========================================================
            // BƯỚC 2: Cập nhật các ID địa lý vào Order
            // (Các trường này phải được thêm vào UpdateAddressRequest)
            // ==========================================================

            // ✅ CẬP NHẬT ID TỈNH/HUYỆN/XÃ VÀO BẢNG ORDER
            order.ToProvinceId = request.ToProvinceId;
            order.ToDistrictId = request.ToDistrictId;
            order.ToWardCode = request.ToWardCode;

            // Đảm bảo TotalCost không bị thay đổi trong hàm này
            // _context.Orders.Update(order); // Entity Framework sẽ tự động theo dõi thay đổi

            await _context.SaveChangesAsync();

            // Trả về Order DTO mới
            var dto = await _context.Orders
                .Include(o => o.EndCustomer)
                .Include(o => o.OrderDetails)
                .AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => _mapper.Map<OrderDto>(o))
                .FirstAsync();

            return dto;
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
        public async Task<bool> SellerApproveOrderDesignAsync(  // update status order
         int orderId,
         ProductionStatus action,
         string sellerId)
        {
            // 1. Tải Order cha và TẤT CẢ OrderDetail cần design
            var order = await _context.Orders
                // Đảm bảo chỉ include các OrderDetail cần thiết kế
                .Include(o => o.OrderDetails.Where(od => od.NeedDesign == true))
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return false;
            }

            // 2. Kiểm tra Quyền
            if (order.SellerUserId != sellerId)
            {
                throw new UnauthorizedAccessException($"Seller {sellerId} is not authorized to modify Order {orderId}.");
            }

            // 3. Kiểm tra trạng thái hiện tại của Order cha
            // Phải đang ở trạng thái CHECK_DESIGN (StatusOrder = 5 là CHECK_DESIGN)
            if (order.StatusOrder != 5)
            {
                throw new InvalidOperationException($"Order {orderId} is not in CHECK_DESIGN status (4). Current status: {order.StatusOrder}.");
            }

            // 4. Kiểm tra ProductionStatus của OrderDetails
            // Tất cả chi tiết phải đang ở CHECK_DESIGN để cho phép duyệt/từ chối
            var allDetailsInCheckDesign = order.OrderDetails.All(od =>
                od.ProductionStatus.GetValueOrDefault() == ProductionStatus.CHECK_DESIGN
            );

            if (!allDetailsInCheckDesign)
            {
                throw new InvalidOperationException($"Not all OrderDetails in Order {orderId} are in CHECK_DESIGN status. Cannot proceed with approval/rejection.");
            }

            // 5. Xác định trạng thái đích cho Order và OrderDetails
            int newOrderStatus;
            ProductionStatus newProductionStatus;
            // thêm message để làm thông báo tới designer
            string designerMessage;
            // lấy DesignerID để gửi thông báo đến
            string designerId = order.OrderDetails.FirstOrDefault()?.AssignedDesignerUserId; // Lấy ID designer

            if (action == ProductionStatus.DESIGN_REDO)
            {
                // Thiết kế lại (Reject)
                newOrderStatus = 6; // Giả định 6 là Order Status cho DESIGN_REDO
                newProductionStatus = ProductionStatus.DESIGN_REDO; // Trạng thái mới cho OrderDetail
                designerMessage = $"Seller has REQUESTED to REDO the design for the order #{orderId}.";
            }
            else if (action == ProductionStatus.READY_PROD)
            {
                // Chốt Đơn (Confirm)
                newOrderStatus = 7; // Giả định 7 là Order Status cho CONFIRMED/READY_PROD
                newProductionStatus = ProductionStatus.READY_PROD; // Trạng thái cuối cho OrderDetail
                designerMessage = $"Seller has ACCEPTED the design for the order #{orderId}.";
            }
            else
            {
                // Trạng thái không hợp lệ đã được Controller chặn, nhưng vẫn kiểm tra phòng hờ
                throw new InvalidOperationException($"Invalid action status {action}. Must be DESIGN_REDO (3) or CONFIRMED (5).");
            }

            // 6. Cập nhật Order cha
            order.StatusOrder = newOrderStatus;

            // 7. Cập nhật HÀNG LOẠT ProductionStatus của các OrderDetail
            foreach (var detail in order.OrderDetails)
            {
                detail.ProductionStatus = newProductionStatus;
            }
            // ✅ BẮT ĐẦU GỬI THÔNG BÁO
            try
            {
                // 1. Gửi thông báo đến Designer
                if (!string.IsNullOrEmpty(designerId))
                {
                    await _notificationService.CreateAndSendNotificationAsync(
                        designerId,
                        designerMessage,
                        $"/designer/tasks" // Hoặc link chi tiết đơn hàng
                    );
                }

                // 2. Gửi cập nhật trạng thái real-time cho group của đơn hàng
                var orderGroupName = $"order_{orderId}";
                await _hubContext.Clients.Group(orderGroupName).SendAsync(
                    "OrderStatusChanged",
                    new { orderId = orderId, newStatus = newProductionStatus.ToString() }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho SellerApproveOrderDesignAsync");
            }
            // ✅ KẾT THÚC GỬI THÔNG BÁO

            // 8. Lưu thay đổi
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> SellerApproveOrderDetailDesignAsync(
            int orderDetailId,
            ProductionStatus action,
            string sellerId,
            string? reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tải Order cha và TẤT CẢ các OrderDetails của nó
                // Chúng ta tìm Order chứa OrderDetailId được yêu cầu
                var order = await _context.Orders
                    .Include(o => o.OrderDetails) // Tải TẤT CẢ các order details liên quan
                    .FirstOrDefaultAsync(o => o.OrderDetails.Any(od => od.OrderDetailId == orderDetailId));

                if (order == null)
                {
                    return false; // Không tìm thấy Order chứa OrderDetailId này
                }

                // Lấy ra OrderDetail cụ thể mà chúng ta đang hành động
                var orderDetail = order.OrderDetails.First(od => od.OrderDetailId == orderDetailId);

                // 2. Kiểm tra Quyền
                if (order.SellerUserId != sellerId)
                {
                    throw new UnauthorizedAccessException($"Seller {sellerId} is not authorized to modify OrderDetail {orderDetailId}.");
                }

                // 3. Kiểm tra trạng thái hiện tại của OrderDetail
                if (orderDetail.ProductionStatus.GetValueOrDefault() != ProductionStatus.CHECK_DESIGN)
                {
                    throw new InvalidOperationException($"OrderDetail {orderDetailId} is not in a modifiable state (Must be CHECK_DESIGN).");
                }

                // 4. Kiểm tra trạng thái đích hợp lệ (Đã có ở Controller, nhưng kiểm tra lại ở Service vẫn tốt)
                if (action != ProductionStatus.DESIGN_REDO && action != ProductionStatus.READY_PROD)
                {
                    throw new InvalidOperationException($"Invalid action status {action}. Must be DESIGN_REDO or READY_PROD.");
                }

                // 5. Ghi Log hành động (từ Kịch bản 1 của bạn)
                string eventType;
                string notificationMessage;

                if (action == ProductionStatus.DESIGN_REDO)
                {
                    eventType = "DESIGN_REJECTED";
                    notificationMessage = $"YÊU CẦU LÀM LẠI thiết kế cho mục #{orderDetailId}. Lý do: {reason}";
                }
                else // (action == ProductionStatus.READY_PROD)
                {
                    eventType = "DESIGN_APPROVED";
                    notificationMessage = $"CHẤP NHẬN thiết kế cho mục #{orderDetailId}.";
                }

                var newLog = new OrderDetailLog
                {
                    OrderDetailId = orderDetailId,
                    ActorUserId = sellerId,
                    EventType = eventType,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                };
                _context.OrderDetailLogs.Add(newLog);

                // 6. Cập nhật ProductionStatus của OrderDetail
                orderDetail.ProductionStatus = action;

                // 7. KIỂM TRA TẤT CẢ ANH EM VÀ CẬP NHẬT ORDER CHA ⭐
                // Kiểm tra xem TẤT CẢ các OrderDetail trong đơn hàng này có cùng trạng thái 'action' hay không
                bool allDetailsMatch = order.OrderDetails.All(od => od.ProductionStatus == action);

                if (allDetailsMatch)
                {
                    if (action == ProductionStatus.DESIGN_REDO)
                    {
                        order.StatusOrder = 6; // 6 = DESIGN_REDO
                    }
                    else if (action == ProductionStatus.READY_PROD)
                    {
                        order.StatusOrder = 7; // 7 = CONFIRMED (Chốt Đơn)
                    }
                }
                // Nếu không phải tất cả đều khớp, trạng thái của Order cha sẽ không thay đổi.

                try
                {
                    string designerId = orderDetail.AssignedDesignerUserId;

                    if (!string.IsNullOrEmpty(designerId))
                    {
                        await _notificationService.CreateAndSendNotificationAsync(
                            designerId,
                            $"Seller đã {notificationMessage}", // Dùng nội dung thông báo mới
                            $"/designer/tasks"
                        );
                    }

                    var orderGroupName = $"order_{order.OrderId}";
                    await _hubContext.Clients.Group(orderGroupName).SendAsync(
                        "OrderStatusChanged",
                        new
                        {
                            orderId = order.OrderId,
                            orderDetailId = orderDetailId,
                            newStatus = action.ToString(),
                            newOrderStatus = order.StatusOrder // Gửi kèm trạng thái tổng của Order
                        }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho SellerApproveOrderDetailDesignAsync");
                }

                // 9. Lưu tất cả thay đổi và commit transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(); // Rất quan trọng: Hoàn tác nếu có lỗi
                throw; // Ném lại lỗi để controller xử lý
            }
        }
        public async Task<bool> SendOrderToReadyProdAsync(int orderId, string sellerId)
        {
            // Sử dụng transaction để đảm bảo cả Order và OrderDetails được cập nhật đồng thời
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    // Include tất cả OrderDetails
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // 1. Kiểm tra Quyền
                if (order.SellerUserId != sellerId)
                {
                    throw new UnauthorizedAccessException($"Seller {sellerId} is not authorized to modify Order {orderId}.");
                }

                // 2. Kiểm tra Trạng thái (Chỉ cho phép từ StatusOrder = 1)
                // Giả định 1 là 'Mới tạo' (hoặc tương đương)
                if (order.StatusOrder != 1)
                {
                    throw new InvalidOperationException($"Chỉ có thể chuyển đơn hàng ở trạng thái 'Mới tạo' (StatusOrder = 1). Trạng thái hiện tại: {order.StatusOrder}.");
                }

                // 3. Cập nhật Order cha
                // Giả định 7 là Order Status cho CONFIRMED/READY_PROD
                const int READY_PROD_ORDER_STATUS = 7;
                order.StatusOrder = READY_PROD_ORDER_STATUS;
                // cập nhật trạng thái order được confirm
                order.OrderDate = DateTime.Now;

                // 4. Cập nhật HÀNG LOẠT ProductionStatus của các OrderDetail
                foreach (var detail in order.OrderDetails)
                {
                    detail.ProductionStatus = ProductionStatus.READY_PROD;
                    // Nếu có trường liên quan đến việc chốt file thiết kế (như IsFinal), bạn cần xử lý tại đây.
                    // Ví dụ: detail.LinkFileDesign = detail.LinkImg; // Giả định dùng ảnh gốc làm file thiết kế
                }
                // ✅ BẮT ĐẦU GỬI THÔNG BÁO
                // Không cần gửi notification (chuông) vì seller là người thực hiện
                // Chỉ cần gửi cập nhật real-time cho ai đang xem
                try
                {
                    var orderGroupName = $"order_{orderId}";
                    await _hubContext.Clients.Group(orderGroupName).SendAsync(
                        "OrderStatusChanged",
                        new { orderId = orderId, newStatus = ProductionStatus.READY_PROD.ToString() }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho SendOrderToReadyProdAsync");
                }
                // ✅ KẾT THÚC GỬI THÔNG BÁO
                // 5. Lưu thay đổi và Commit Transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đơn hàng ID {OrderId} sang READY_PROD.", orderId);
                await transaction.RollbackAsync();
                throw; // Ném lại lỗi để Controller xử lý
            }
        }

        public async Task<ApproveOrderResult> ApproveOrderForShippingAsync(int orderId)
        {
            var order = await _context.Orders
                                      .Include(o => o.OrderDetails)
                                        .ThenInclude(od => od.ProductVariant)
                                        .ThenInclude(pv => pv.Product)
                                      .Include(o => o.EndCustomer)
                                      .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return new ApproveOrderResult { IsSuccess = false, OrderFound = false };
            }

            if (order.OrderDetails == null || !order.OrderDetails.Any())
            {
                return new ApproveOrderResult
                {
                    IsSuccess = false,
                    CanApprove = false,
                    ErrorMessage = "Order has no product details."
                };
            }

            if (order.EndCustomer == null)
            {
                return new ApproveOrderResult { IsSuccess = false, ErrorMessage = "Customer information is missing." };
            }

            // Validate District/Ward
            if (order.ToDistrictId == null || string.IsNullOrEmpty(order.ToWardCode))
            {
                return new ApproveOrderResult { IsSuccess = false, ErrorMessage = "Missing District ID or Ward Code for shipping." };
            }

            bool allDetailsQcDone = order.OrderDetails.All(d => d.ProductionStatus == ProductionStatus.QC_DONE);

            if (!allDetailsQcDone)
            {
                return new ApproveOrderResult
                {
                    IsSuccess = false,
                    CanApprove = false,
                    ErrorMessage = "Not all products have passed QC (Status QC_DONE)."
                };
            }

            var orderShippingRequest = new CreateOrderRequest
            {
                ToName = order.EndCustomer.Name,
                ToPhone = order.EndCustomer.Phone,
                ToAddress = order.EndCustomer.Address,
                ToDistrictId = order.ToDistrictId.Value,
                ToWardCode = order.ToWardCode,
                WeightInGrams = (int)Math.Ceiling(order.OrderDetails.Sum(od => (od.ProductVariant.WeightGram ?? 0m) * od.Quantity)),
                Length = (int)Math.Ceiling(order.OrderDetails.Max(od => od.ProductVariant.LengthCm ?? 0m)),
                Width = (int)Math.Ceiling(order.OrderDetails.Max(od => od.ProductVariant.WidthCm ?? 0m)),
                Height = (int)Math.Ceiling(order.OrderDetails.Sum(od => (od.ProductVariant.HeightCm ?? 0m) * od.Quantity)),
                Items = order.OrderDetails.Select(od => new OrderItemRequest
                {
                    Name = od.ProductVariant.Product.ProductName,
                    Quantity = od.Quantity,
                    Weight = (int)Math.Ceiling((od.ProductVariant.WeightGram ?? 0m) * od.Quantity)
                }).ToList()
            };


            try
            {
                var shippingResult = await _shippingService.CreateOrderAsync(orderShippingRequest);
                if (shippingResult == null || string.IsNullOrEmpty(shippingResult.OrderCode))
                {
                    return new ApproveOrderResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Lỗi tạo đơn vận chuyển: Không nhận được Mã Vận Đơn từ hệ thống."
                    };
                }
                order.Tracking = shippingResult.OrderCode;
                order.StatusOrder = 13; // OrderStatus.Shipping;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                // ✅ BẮT ĐẦU GỬI THÔNG BÁO
                try
                {
                    // 1. Gửi thông báo (chuông) cho Seller
                    await _notificationService.CreateAndSendNotificationAsync(
                        order.SellerUserId,
                        $"Đơn hàng #{order.OrderCode} của bạn đang tiến hành giao. Mã vận đơn: {order.Tracking}",
                        $"/seller/orders/{orderId}"
                    );

                    // 2. Gửi cập nhật real-time
                    var orderGroupName = $"order_{orderId}";
                    await _hubContext.Clients.Group(orderGroupName).SendAsync(
                        "OrderStatusChanged",
                        new { orderId = orderId, newStatus = "SHIPPING", trackingCode = order.Tracking } // Giả sử 13 là Shipping
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho ApproveOrderForShippingAsync");
                }
                // ✅ KẾT THÚC GỬI THÔNG BÁO
                return new ApproveOrderResult { IsSuccess = true };
            }
            // Bắt lỗi 400 (Bad Request)
            // Bắt lỗi 400 (Bad Request)
            catch (Exception ex) when (ex.Message.Contains("400") || (ex is System.Net.Http.HttpRequestException httpEx && httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest))
            {
                try
                {
                    // 1. Phân tích nguyên nhân cụ thể từ Message lỗi
                    string reasonDetail = "Địa chỉ hoặc SĐT không hợp lệ"; // Mặc định
                    string logReason = "Likely invalid Address or Phone";

                    // Kiểm tra các từ khóa lỗi thường gặp của GHN/GHTK
                    if (ex.Message.Contains("PHONE_INVALID") || ex.Message.Contains("phone"))
                    {
                        reasonDetail = "Số điện thoại người nhận không đúng định dạng (thừa/thiếu số hoặc đầu số lạ)";
                        logReason = "Invalid Phone Number";
                    }
                    else if (ex.Message.Contains("WARD") || ex.Message.Contains("DISTRICT") || ex.Message.Contains("ADDRESS"))
                    {
                        reasonDetail = "Địa chỉ (Phường/Xã hoặc Quận/Huyện) không khớp với hệ thống vận chuyển";
                        logReason = "Invalid Address/Geography";
                    }

                    // 2. Cập nhật trạng thái về 19 (CHANGE_ADDRESS)
                    order.StatusOrder = 19;
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();

                    // 3. Gửi thông báo chi tiết cho Seller
                    string notificationMsg = $"Tạo đơn vận chuyển thất bại: {reasonDetail}. Đơn hàng #{order.OrderCode} đã chuyển sang trạng thái chờ. Vui lòng cập nhật lại.";

                    await _notificationService.CreateAndSendNotificationAsync(
                        order.SellerUserId,
                        notificationMsg,
                        $"/seller/orders/{orderId}"
                    );

                    // Log lỗi hệ thống
                    _logger.LogWarning(ex, $"Shipping API returned 400 for Order {orderId}. Reason: {logReason}.");

                    return new ApproveOrderResult
                    {
                        IsSuccess = false,
                        // Trả về FE thông báo cụ thể luôn
                        ErrorMessage = notificationMsg
                    };
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Lỗi khi cập nhật trạng thái chờ cho đơn hàng " + orderId);
                    return new ApproveOrderResult { IsSuccess = false, ErrorMessage = "Lỗi nghiêm trọng: Không thể cập nhật trạng thái đơn hàng sau khi API vận chuyển lỗi." };
                }
            }
            catch (DbUpdateException ex)
            {
                return new ApproveOrderResult { IsSuccess = false, ErrorMessage = "Database error occurred while updating the order status." };
            }
            catch (Exception ex)
            {
                return new ApproveOrderResult { IsSuccess = false, ErrorMessage = "An unexpected error occurred." };
            }
        }

        public async Task<OrderStatsDto> GetOrderStatsForSellerAsync(string sellerUserId)
        {
            var statusCounts = await _context.Orders
                .AsNoTracking()
                .Where(o => o.SellerUserId == sellerUserId)
                .GroupBy(o => o.StatusOrderNavigation.Code)   // Dùng status CODE
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var dict = statusCounts
                .Where(x => !string.IsNullOrEmpty(x.Status))
                .ToDictionary(x => x.Status!, x => x.Count);

            int total = dict.Values.Sum();


            // ================== PHASES ==================
            var designPhase = new[]
            {
                "DRAFT", "NEEDDESIGN", "DESIGNING",
                "CHECKDESIGN", "DESIGN_REDO", "CONFIRMED"
            };

            var manufacturePhase = new[]
            {
                "READY_PROD", "INPROD", "FINISHED",
                "PROD_REWORK", "QC_DONE", "QC_FAIL"
            };

            var deliveryPhase = new[]
            {
                "SHIPPING", "SHIPPED"
            };

            var refundPhase = new[]
            {
                "HOLD_RF", "HOLD_RP", "REFUND"
            };

            // ================== GROUP OBJECT ==================
            var stageGroups = new Dictionary<string, List<OrderStatsDto.StatusCountItem>>
            {
                ["Design Phase"] = designPhase
          .Select(s => new OrderStatsDto.StatusCountItem
          {
              Status = s,
              Count = dict.ContainsKey(s) ? dict[s] : 0
          })
          .ToList(),

                ["Manufacture Phase"] = manufacturePhase
          .Select(s => new OrderStatsDto.StatusCountItem
          {
              Status = s,
              Count = dict.ContainsKey(s) ? dict[s] : 0
          })
          .ToList(),

                ["Delivery Phase"] = deliveryPhase
          .Select(s => new OrderStatsDto.StatusCountItem
          {
              Status = s,
              Count = dict.ContainsKey(s) ? dict[s] : 0
          })
          .ToList(),

                ["Refund / Reprint Phase"] = refundPhase
          .Select(s => new OrderStatsDto.StatusCountItem
          {
              Status = s,
              Count = dict.ContainsKey(s) ? dict[s] : 0
          })
          .ToList()
            };


            // ================== RETURN DTO ==================
            return new OrderStatsDto
            {
                Total = total,
                StatusCounts = dict,
                NeedActionCount = 0,
                UrgentCount = 0,
                CompletedCount = 0,
                StageGroups = stageGroups
            };
        }

        public async Task<(IEnumerable<OrderWithDetailsDto> Orders, int Total)> GetFilteredAndPagedOrdersAsync(
            string? status,
            string? searchTerm,
            string? sortColumn,
            string? sellerId,
            string? sortDirection,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            // ví dụ code
            var query = _context.Orders
                .Include(o => o.EndCustomer)
                .Include(o => o.StatusOrderNavigation)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.StatusOrderNavigation.Code == status);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(o =>
                    o.OrderCode.Contains(searchTerm) ||
                    o.EndCustomer.Name.Contains(searchTerm));

            if (fromDate.HasValue && toDate.HasValue)
                query = query.Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate);
            if (!string.IsNullOrEmpty(sellerId))
            {
                // Hàm này sẽ lọc theo SellerUserId trên bảng Order
                query = query.Where(o => o.SellerUserId == sellerId);
            }
            int total = await query.CountAsync();
            // --- 3. SẮP XẾP (SORTING) ---
            // Kiểm tra hướng sắp xếp
            bool isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            // Ánh xạ 'sortColumn' string sang biểu thức OrderBy
            // Chúng ta gán lại biến 'query' với phiên bản đã được sắp xếp
            switch (sortColumn?.ToLower())
            {
                case "ordercode":
                    query = isAscending
                        ? query.OrderBy(o => o.OrderCode)
                        : query.OrderByDescending(o => o.OrderCode);
                    break;

                case "customername": // Sắp xếp theo bảng quan hệ
                    query = isAscending
                        ? query.OrderBy(o => o.EndCustomer.Name)
                        : query.OrderByDescending(o => o.EndCustomer.Name);
                    break;

                case "totalcost":
                    query = isAscending
                        ? query.OrderBy(o => o.TotalCost)
                        : query.OrderByDescending(o => o.TotalCost);
                    break;

                case "orderdate":
                default: // Mặc định sắp xếp theo OrderDate (ngày đặt hàng)
                    query = isAscending
                        ? query.OrderBy(o => o.OrderDate)
                        : query.OrderByDescending(o => o.OrderDate);
                    break;
            }
            var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderWithDetailsDto
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                OrderDate = o.OrderDate,
                CustomerId = o.EndCustomerId,
                CustomerName = o.EndCustomer.Name,
                Phone = o.EndCustomer.Phone,
                Email = o.EndCustomer.Email,
                Address = o.EndCustomer.Address,
                SellerId = o.SellerUserId,
                SellerName = o.SellerUser.FullName,
                CreationDate = o.CreationDate ?? o.OrderDate,
                TotalCost = o.TotalCost,
                PaymentStatus = o.PaymentStatus,
                ProductionStatus = o.ProductionStatus,
                ActiveTTS = o.ActiveTts,
                Tracking = o.Tracking,
                StatusOrder = o.StatusOrder,
                StatusOderName = o.StatusOrderNavigation.Code,

                // --- ⭐ THÊM LOGIC TÍNH TOÁN CÁC TRƯỜNG MỚI ---

                // Lấy lý do YÊU CẦU (của Seller)
                Reason = (o.StatusOrder == 16) // 16 = HOLD
                    ? (from cr in _context.CancellationRequests
                       where cr.OrderId == o.OrderId
                       orderby cr.CreatedAt descending
                       select cr.RequestReason).FirstOrDefault()
                : (o.StatusOrder == 18) // 18 = REFUNDED
                    ? (from rf in _context.Refunds
                       where rf.OrderId == o.OrderId
                       orderby rf.CreatedAt descending
                       select rf.Reason).FirstOrDefault()
                : null,

                // Lấy lý do TỪ CHỐI (của Staff)
                RejectionReason = (
                    (from cr in _context.CancellationRequests
                     where cr.OrderId == o.OrderId && cr.Status == "Rejected"
                     orderby cr.ReviewedAt descending
                     select cr.RejectionReason).FirstOrDefault()
                ) ?? (
                    (from rf in _context.Refunds
                     where rf.OrderId == o.OrderId && rf.Status == "Rejected"
                     orderby rf.ReviewedAt descending
                     select rf.StaffRejectionReason).FirstOrDefault()
                ),

                // Lấy thông tin Refund mới nhất (nếu có)
                LatestRefundId = (from rf in _context.Refunds
                                  where rf.OrderId == o.OrderId
                                  orderby rf.CreatedAt descending
                                  select (int?)rf.RefundId).FirstOrDefault(), // (int?) để cho phép null

                RefundAmount = (from rf in _context.Refunds
                                where rf.OrderId == o.OrderId
                                orderby rf.CreatedAt descending
                                select (decimal?)rf.Amount).FirstOrDefault(), // (decimal?) để cho phép null

                // ⭐ ĐÁNH DẤU TRUE NẾU CÓ 1 YÊU CẦU ĐANG "PENDING"
                IsRefundPending = _context.Refunds.Any(r => r.OrderId == o.OrderId && r.Status == "Pending")
            }).ToListAsync();
            return (orders, total);
        }
        public async Task<(IEnumerable<OrderWithDetailsDto> Orders, int Total)> GetFilteredAndPagedOrdersForInvoiceAsync(
            string? status, // status theo CODE
            string? searchTerm,
            string? seller,
            string? sortColumn,
            string? sortDirection,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            // Bắt đầu query
            var query = _context.Orders
                .Include(o => o.EndCustomer)
                .Include(o => o.StatusOrderNavigation)
                .Include(o => o.SellerUser)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                .Where(o =>
                    o.StatusOrder >= 10 &&
                    o.StatusOrder <= 15 &&
                    !o.OrderDetails.Any(od => od.ProductionStatus == null || od.ProductionStatus < ProductionStatus.FINISHED)
                )
                .AsQueryable();

            // 1. Lọc theo Status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.StatusOrderNavigation.Code == status);
            }

            // 2. Lọc theo Search Term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o =>
                    o.OrderCode.Contains(searchTerm) ||
                    o.EndCustomer.Name.Contains(searchTerm));
            }

            // 3. Lọc theo Date Range
            if (fromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                // Logic < toDate (đã +1 ngày ở frontend)
                query = query.Where(o => o.OrderDate < toDate.Value);
            }

            var projectedQuery = query
                .ProjectTo<OrderWithDetailsDto>(_mapper.ConfigurationProvider);

            if (!string.IsNullOrEmpty(seller))
            {
                projectedQuery = projectedQuery.Where(dto => dto.SellerId == seller ||
                                                     dto.SellerName == seller);
            }

            // 5. Lấy tổng số (sau khi đã lọc)
            int total = await projectedQuery.CountAsync();

            // 6. Sắp xếp (Sorting)
            // Triển khai sắp xếp động
            if (!string.IsNullOrEmpty(sortColumn))
            {
                // Mặc định là 'asc' nếu không có sortDirection
                var isAscending = sortDirection?.ToLower() == "asc";

                // Sử dụng Expression để sắp xếp động an toàn
                // Thêm các cột bạn muốn hỗ trợ
                Expression<Func<OrderWithDetailsDto, object>> keySelector = sortColumn.ToLower() switch
                {
                    "orderdate" => dto => dto.OrderDate,
                    "customername" => dto => dto.CustomerName,
                    "totalcost" => dto => dto.TotalCost,
                    _ => dto => dto.OrderDate // Mặc định
                };

                projectedQuery = isAscending
                    ? projectedQuery.OrderBy(keySelector)
                    : projectedQuery.OrderByDescending(keySelector);
            }
            else
            {
                // Sắp xếp mặc định
                projectedQuery = projectedQuery.OrderByDescending(dto => dto.OrderDate);
            }

            // 7. Phân trang (Pagination)
            var orders = await projectedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, total);
        }

        public async Task<IEnumerable<string>> GetUniqueSellersAsync(string? status)
        {
            // Logic này được chuyển từ controller (ở câu trả lời trước)
            var query = _context.Orders
                .Include(o => o.StatusOrderNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.StatusOrderNavigation.Code == status);
            }

            var sellerNames = await query
                .ProjectTo<OrderWithDetailsDto>(_mapper.ConfigurationProvider)
                .Select(dto => dto.SellerName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();

            return sellerNames;
        }
        public async Task<OrderWithDetailsDto?> GetManagerOrderDetailAsync(int orderId)
        {
            // BƯỚC 1: Lấy thông tin Order gốc (Giữ nguyên các Include cũ, KHÔNG Include Refund/Cancel)
            var orderEntity = await _context.Orders
                .Include(o => o.EndCustomer)
                .Include(o => o.StatusOrderNavigation)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (orderEntity == null) return null;

            // BƯỚC 2: Map sang DTO (Sử dụng AutoMapper như bình thường)
            var dto = _mapper.Map<OrderWithDetailsDto>(orderEntity);

            // BƯỚC 3: Truy vấn thủ công bảng Refunds (Tìm theo OrderId)
            // Vì không có Navigation Property nên ta query trực tiếp từ DbSet _context.Refunds
            var latestRefund = await _context.Refunds
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            // BƯỚC 5: Logic điền dữ liệu vào DTO (Mapping thủ công các trường manager cần)

            // --- Xử lý logic Hoàn tiền (Refund) ---
            if (latestRefund != null)
            {
                dto.LatestRefundId = latestRefund.RefundId;
                dto.IsRefundPending = (latestRefund.Status == "Pending"); // Hoặc check null tùy logic của bạn
                dto.RefundAmount = latestRefund.Amount;

                // Reason: Lý do Seller/Khách yêu cầu
                dto.Reason = latestRefund.Reason;
                dto.ProofUrl = latestRefund.ProofUrl;
                // RejectionReason: Lý do Staff từ chối
                dto.RejectionReason = latestRefund.StaffRejectionReason;

                // Nếu trạng thái Order đang là Refunded hoặc Refund Pending, ưu tiên hiển thị lý do Refund

            }
            return dto;
        }

        public async Task<OrderImportResult> ImportFromExcelAsync(IFormFile file, string sellerUserId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File rỗng.");

            await _cache.LoadAsync(); // Load cache trước

            var result = new OrderImportResult();
            var validRows = new List<OrderImportRowDto>(); // List tạm để chứa các dòng hợp lệ

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) throw new Exception("Không có sheet dữ liệu.");

            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);
            result.TotalRows = rows.Count();

            // BƯỚC 1: Đọc và Validate toàn bộ các dòng
            foreach (var row in rows)
            {
                var dto = MapRowToDto(row.WorksheetRow());

                var validationResult = await _validator.ValidateAsync(dto);
                if (!validationResult.IsValid)
                {
                    result.Errors.Add(new OrderImportRowError
                    {
                        RowNumber = dto.RowNumber,
                        Messages = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
                    });
                    // Dòng lỗi thì không add vào validRows
                    continue;
                }

                validRows.Add(dto);
            }

            // BƯỚC 2: Gom nhóm theo OrderCode (Logic gộp đơn)
            // GroupBy trả về danh sách các nhóm, mỗi nhóm có Key là OrderCode
            var orderGroups = validRows.GroupBy(x => x.OrderCode);

            // BƯỚC 3: Tạo Order từ từng nhóm
            foreach (var group in orderGroups)
            {
                try
                {
                    // Gọi hàm factory mới xử lý cả nhóm
                    var orderEntity = await _orderFactory.CreateOrderFromGroupAsync(group, sellerUserId);

                    _context.Orders.Add(orderEntity);
                    result.SuccessCount++; // Tính là 1 đơn thành công (dù gồm nhiều dòng)
                }
                catch (Exception ex)
                {
                    // Nếu lỗi khi tạo đơn, báo lỗi cho tất cả các dòng trong nhóm đó
                    result.Errors.Add(new OrderImportRowError
                    {
                        RowNumber = group.First().RowNumber, // Lấy dòng đầu đại diện
                        Messages = new List<string> { $"Lỗi gộp đơn {group.Key}: {ex.Message}" }
                    });
                }
            }

            // BƯỚC 4: Lưu Database
            if (result.SuccessCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return result;
        }

        // Tách hàm Map cho gọn code
        private OrderImportRowDto MapRowToDto(IXLRow row)
        {
            return new OrderImportRowDto
            {
                RowNumber = row.RowNumber(),
                OrderCode = GetString(row.Cell(1)),
                CustomerName = GetString(row.Cell(2)),
                Phone = GetString(row.Cell(3)),
                Email = GetString(row.Cell(4)),
                Address = GetString(row.Cell(5)),
                Province = GetString(row.Cell(6)),
                District = GetString(row.Cell(7)),
                Ward = GetString(row.Cell(8)),
                SKU = GetString(row.Cell(9)),
                Quantity = GetInt(row.Cell(10), 1),
                Accessory = GetString(row.Cell(11)),
                Note = GetString(row.Cell(12)),
                LinkImg = GetString(row.Cell(13)),
                LinkThanksCard = GetString(row.Cell(14)),
                LinkFileDesign = GetString(row.Cell(15))
            };
        }

        private static string? GetString(IXLCell cell)
        {
            if (cell == null || cell.IsEmpty()) return null;
            return cell.GetString().Trim();
        }

        private static int GetInt(IXLCell cell, int defaultValue = 0)
        {
            if (cell == null || cell.IsEmpty()) return defaultValue;
            if (cell.DataType == XLDataType.Number) return (int)cell.GetDouble();
            var text = cell.GetString().Trim();
            return int.TryParse(text, out var val) ? val : defaultValue;
        }
        // --- PHASE 1: ĐỌC FILE VÀ VALIDATE (KHÔNG LƯU DB) ---
        public async Task<List<OrderImportRowDto>> ValidateImportAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("File is empty.");

            await _cache.LoadAsync();
            var resultList = new List<OrderImportRowDto>();

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) throw new Exception("Excel file has no sheets.");

            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ header

            foreach (var row in rows)
            {
                var dto = MapRowToDto(row.WorksheetRow());

                // Thực hiện Validate
                var validationResult = await _validator.ValidateAsync(dto);

                if (!validationResult.IsValid)
                {
                    dto.IsValid = false;
                    dto.Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                }
                else
                {
                    dto.IsValid = true;
                }

                resultList.Add(dto);
            }

            return resultList;
        }

        // --- PHASE 2: NHẬN JSON ĐÃ SỬA VÀ LƯU DB ---
        public async Task<OrderImportResult> ConfirmImportAsync(List<OrderImportRowDto> dtos, string sellerUserId)
        {
            var result = new OrderImportResult();
            var rowsWithErrors = new List<OrderImportRowDto>();

            await _cache.LoadAsync();

            // 1. Validate lại toàn bộ list
            foreach (var dto in dtos)
            {
                var valResult = await _validator.ValidateAsync(dto);
                if (!valResult.IsValid)
                {
                    // Thay vì throw Exception, ta gom lỗi lại
                    result.Errors.Add(new OrderImportRowError
                    {
                        RowNumber = dto.RowNumber,
                        Messages = valResult.Errors.Select(e => e.ErrorMessage).ToList()
                    });
                }
            }

            // 2. Nếu có bất kỳ dòng nào lỗi -> TRẢ VỀ NGAY, KHÔNG LƯU
            if (result.Errors.Any())
            {
                result.SuccessCount = 0;
                result.TotalRows = dtos.Count;
                return result; // Frontend sẽ nhận được list Errors này để tô đỏ lại
            }

            // Gom nhóm theo OrderCode (Logic cũ của bạn)
            var orderGroups = dtos.GroupBy(x => x.OrderCode);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var group in orderGroups)
                {
                    // Logic tạo Order từ Group (đã viết trước đó)
                    var orderEntity = await _orderFactory.CreateOrderFromGroupAsync(group, sellerUserId);
                    _context.Orders.Add(orderEntity);
                    result.SuccessCount++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }

            result.TotalRows = dtos.Count;
            return result;
        }
        public async Task<OrderActivityDto?> GetOrderActivityTimelineAsync(int orderId)
        {
            // 1. Truy vấn Order chính và các OrderDetails nhẹ để lấy các ID cần thiết
            var orderData = await _context.Orders
                .Include(o => o.OrderDetails) // Cần Include để lấy OrderDetailID
                .Where(o => o.OrderId == orderId)
                .Select(o => new
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    CreationDate = o.CreationDate,
                    OrderDate = o.OrderDate,
                    // Đảm bảo lấy OrderDetailID đúng cách
                    OrderDetailIds = o.OrderDetails.Select(od => od.OrderDetailId).ToList()
                })
                .FirstOrDefaultAsync();

            if (orderData == null) return null;

            var orderActivityDto = new OrderActivityDto
            {
                CreationDate = orderData.CreationDate,
                OrderDate = orderData.OrderDate
            };

            var allRefunds = await _context.Refunds
        // Phải Include OrderDetail để lấy thông tin sản phẩm (ProductName, Sku, Price)
        .Include(r => r.OrderDetail)
        .Where(r => r.OrderId == orderId)
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

            orderActivityDto.AllRefunds = allRefunds.Select(r =>
            {
                // 🚨 CHÚ Ý: Vì mỗi Refund chỉ là 1 Item, TotalRequestedAmount = Amount
                var refundItemDetails = new RefundItemDetailsDto
                {
                    OrderDetailId = r.OrderDetailId ?? 0,
                 //   ProductName = r.OrderDetail?.ProductVariant.Product.ProductName ?? "N/A",
                 //   Sku = r.OrderDetail?.ProductVariant.Sku ?? "N/A",
                    Quantity = r.OrderDetail?.Quantity ?? 0,
                    OriginalPrice = r.OrderDetail?.Price ?? 0,
                    RefundAmount = r.Amount // Số tiền yêu cầu hoàn
                };

                return new RefundDetailsDto
                {
                    RefundId = r.RefundId,
                    OrderId = r.OrderId,
                    OrderCode = orderData.OrderCode ?? "N/A",
                    Status = r.Status,
                    TotalRequestedAmount = r.Amount, // Total = Amount vì chỉ có 1 item
                    Reason = r.Reason,
                    ProofUrl = r.ProofUrl,
                    StaffRejectionReason = r.StaffRejectionReason,
                    CreatedAt = r.CreatedAt,
                    ReviewedAt = r.ReviewedAt,
                    // ✨ Tạo danh sách chỉ với Item duy nhất này ✨
                    Items = new List<RefundItemDetailsDto> { refundItemDetails }
                };
            }).ToList();


            // ----------------------------------------------------------------------
            // --- 3. XỬ LÝ REPRINT CHI TIẾT (TỔNG HỢP CẤP ORDER) ---
            // ----------------------------------------------------------------------

            // Sử dụng OrderDetailIds từ bước 1 để truy vấn Reprints
            var reprints = await _context.Reprints
                .Include(r => r.OriginalOrderDetail)
                // Kiểm tra xem OrderDetailIds có dữ liệu trước khi dùng Contains
                .Where(r => orderData.OrderDetailIds != null && orderData.OrderDetailIds.Contains(r.OriginalOrderDetailId))
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            orderActivityDto.AllReprints = reprints.Select(r => new ReprintDetailsDto
            {
                Id = r.Id,

                // ✨ FIX 1: Dùng ?. trên OriginalOrderDetail ✨
             //   OrderId = r.OriginalOrderDetail?.OrderId ?? 0,

               // OrderCode = orderData.OrderCode ?? "N/A",
                Status = r.Status,
                Reason = r.Reason,
                ProofUrl = r.ProofUrl,
                RejectionReason = r.StaffRejectionReason,
                RequestDate = r.RequestDate,

                RequestedItems = new List<ReprintItemDto>
        {
            new ReprintItemDto
            {
                OrderDetailId = r.OriginalOrderDetailId,
            
                // ✨ FIX 2: Dùng ?. trên OriginalOrderDetail ✨
             //   ProductName = r.OriginalOrderDetail?.ProductVariant.Product.ProductName ?? "Unknown Product",
              //  SKU = r.OriginalOrderDetail?.ProductVariant.Sku ?? "N/A",
                Quantity = r.OriginalOrderDetail?.Quantity ?? 0, // Sử dụng ?? 0 cho kiểu int
                ReprintSelected = true
            }
        }
                }).ToList();

            return orderActivityDto;
        }

    }
}