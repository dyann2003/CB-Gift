// Trong OrderService.cs

using CB_Gift.Data;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;
using CB_Gift.Models.Enums;
using System.Linq;
using System.Collections.Generic; // Cần thiết cho List

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

        // ----------------------------------------------------------------------
        // ✅ 1. TRIỂN KHAI GET DASHBOARD STATS (TỐI ƯU)
        // ----------------------------------------------------------------------
        public async Task<DashboardStatsDto> GetDashboardStatsForSellerAsync(string sellerUserId)
        {
            // ĐỊNH NGHĨA CÁC NHÓM TRẠNG THÁI (Sử dụng tên Tiếng Việt để đồng bộ với FE)
            var designStatuses = new[] { "Draft (Nháp)", "Cần Design", "Đang làm Design", "Cần Check Design" };
            var productionReadyStatuses = new[] { "Sẵn sàng Sản xuất", "Sản xuất Xong" };

            var urgentIssueStatuses = new[] { "Sản xuất Lại", "Thiết kế Lại (Design Lỗi)" };

            var completedStatuses = new[] { "Đã Ship", "Đã Kiểm tra Chất lượng" };
            var closedStatuses = new[] { "Chốt Đơn (Khóa Seller)" };
            var excludedStatuses = new[] { "Cancel", "Hoàn Hàng" };


            // 1. CHỈ TRUY VẤN LẤY STATUS (Rất hiệu quả về mặt hiệu suất DB)
            var allOrderStatuses = await _context.Orders
                .Include(o => o.StatusOrderNavigation)
                .Where(o => o.SellerUserId == sellerUserId)
                .Select(o => o.StatusOrderNavigation.NameVi) // Chỉ lấy trường NameVi
                .ToListAsync();

            // 2. Tính toán các Stats trên bộ nhớ (rất nhanh)
            int totalOrders = allOrderStatuses.Count;

            int completed = allOrderStatuses.Count(s => completedStatuses.Contains(s));
            int urgentIssues = allOrderStatuses.Count(s => urgentIssueStatuses.Contains(s));

            // CẦN HÀNH ĐỘNG = (Design Action) + (Ready Prod Action) + (Closed Order)
            int needsAction = allOrderStatuses.Count(s =>
                designStatuses.Contains(s) ||
                productionReadyStatuses.Contains(s) ||
                closedStatuses.Contains(s) // Giả định Chốt Đơn vẫn là cần hành động từ góc độ Seller
            );

            return new DashboardStatsDto
            {
                TotalOrders = totalOrders,
                NeedsAction = needsAction,
                UrgentIssues = urgentIssues,
                Completed = completed
            };
        }

        // ----------------------------------------------------------------------
        // ✅ 2. TRIỂN KHAI GET FILTERED AND PAGED ORDERS
        // ----------------------------------------------------------------------
        public async Task<(List<OrderWithDetailsDto> Orders, int TotalCount)> GetFilteredAndPagedOrdersForSellerAsync(
            string sellerUserId,
            string? status,
            string? searchTerm,
            string? sortColumn,
            string? sortDirection,
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
                query = query.Where(o => o.StatusOrderNavigation.NameVi == status);

            // 2. Xử lý Tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                string term = searchTerm.ToLower();
                query = query.Where(o =>
                    (o.OrderCode != null && o.OrderCode.ToLower().Contains(term)) ||
                    (o.EndCustomer != null && o.EndCustomer.Name.ToLower().Contains(term))
                // ❌ Loại bỏ ProductVariant Name để tránh lỗi biên dịch
                );
            }

            // 3. Đếm tổng số lượng (sau khi lọc, trước khi phân trang)
            var totalCount = await query.CountAsync();

            // 4. Xử lý Sắp xếp
            if (!string.IsNullOrEmpty(sortColumn))
            {
                query = sortColumn.ToLower() switch
                {
                    "orderid" => (sortDirection.ToLower() == "asc" ? query.OrderBy(o => o.OrderCode) : query.OrderByDescending(o => o.OrderCode)),
                    "orderdate" => (sortDirection.ToLower() == "asc" ? query.OrderBy(o => o.OrderDate) : query.OrderByDescending(o => o.OrderDate)),
                    "customername" => (sortDirection.ToLower() == "asc" ? query.OrderBy(o => o.EndCustomer.Name) : query.OrderByDescending(o => o.EndCustomer.Name)),
                    "totalamount" => (sortDirection.ToLower() == "asc" ? query.OrderBy(o => o.TotalCost) : query.OrderByDescending(o => o.TotalCost)),
                    _ => query.OrderByDescending(o => o.CreationDate ?? o.OrderDate)
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
        // Giữ nguyên các phương thức còn lại
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
                .Include(o => o.EndCustomer)
                .Include(o => o.StatusOrderNavigation)
                 .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                .Where(o => o.OrderId == orderId);

            if (!string.IsNullOrEmpty(sellerUserId))
            {
                query = query.Where(o => o.SellerUserId == sellerUserId);
            }

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
            var details = await _context.OrderDetails
                .Include(d => d.ProductVariant)
                .Where(d => d.OrderId == orderId)
                .ToListAsync();
            Console.WriteLine(details.ToString());
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
            decimal itemBase = details.Sum(d =>
            {
                var pv = d.ProductVariant;
                decimal baseCost = pv.BaseCost ?? 0;
                return d.Quantity * baseCost;
            });
            decimal baseShip = details.Max(d => d.ProductVariant.ShipCost ?? 0);
            decimal maxExtra = details.Max(d => d.ProductVariant.ExtraShipping ?? 0);
            decimal totalQty = details.Sum(d => d.Quantity);
            decimal totalCost = itemBase + baseShip;
            if (totalQty > 1)
            {
                totalCost += (totalQty - 1) * maxExtra;
            }
            var orderToUpdate = await _context.Orders.FindAsync(orderId);
            if (orderToUpdate != null)
            {
                orderToUpdate.TotalCost += totalCost;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<MakeOrderResponse> MakeOrder(MakeOrderDto request, string sellerUserId)
        {
            int customerId;
            var newEndCustomer = _mapper.Map<EndCustomer>(request.CustomerInfo);
            _context.EndCustomers.Add(newEndCustomer);
            await _context.SaveChangesAsync();
            customerId = newEndCustomer.CustId;
            var order = _mapper.Map<Order>(request.OrderCreate);
            order.SellerUserId = sellerUserId;
            order.EndCustomerId = customerId;
            order.OrderDate = DateTime.Now;
            order.ProductionStatus = "Created";
            order.StatusOrder = 1;
            decimal totalCost = 0;
            if (order.ActiveTts == true)
            {
                totalCost += 1;
            }
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
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
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerUserId == sellerUserId);

            if (order == null)
            {
                throw new KeyNotFoundException("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.");
            }

            if (order.StatusOrder != 1)
            {
                throw new InvalidOperationException("Chỉ có thể cập nhật đơn hàng ở trạng thái 'Mới tạo'.");
            }

            var customer = await _context.EndCustomers.FindAsync(order.EndCustomerId);
            if (customer != null) _mapper.Map(request.CustomerInfo, customer);
            _mapper.Map(request.OrderUpdate, order);

            var detailsInRequest = request.OrderDetailsUpdate ?? new List<OrderDetailUpdateRequest>();
            var requestDetailIds = detailsInRequest.Select(d => d.OrderDetailID).ToHashSet();

            var detailsToRemove = order.OrderDetails
                .Where(d => !requestDetailIds.Contains(d.OrderDetailId))
                .ToList();
            if (detailsToRemove.Any())
            {
                _context.OrderDetails.RemoveRange(detailsToRemove);
            }

            foreach (var detailDto in detailsInRequest)
            {
                if (detailDto.OrderDetailID > 0)
                {
                    var existingDetail = order.OrderDetails
                        .FirstOrDefault(d => d.OrderDetailId == detailDto.OrderDetailID);
                    if (existingDetail != null)
                    {
                        _mapper.Map(detailDto, existingDetail);
                    }
                }
                else
                {
                    var newDetail = _mapper.Map<OrderDetail>(detailDto);
                    newDetail.OrderId = order.OrderId;
                    newDetail.CreatedDate = DateTime.UtcNow;
                    order.OrderDetails.Add(newDetail);
                }
            }

            await _context.SaveChangesAsync();
            await RecalculateOrderTotalCost(order.OrderId);

            var updatedOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            return _mapper.Map<MakeOrderResponse>(updatedOrder);
        }
        public async Task<bool> DeleteOrderAsync(int orderId, string sellerUserId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.SellerUserId == sellerUserId);

            if (order == null)
            {
                return false;
            }

            if (order.StatusOrder != 1)
            {
                throw new InvalidOperationException("Chỉ có thể xóa đơn hàng ở trạng thái 'Daft(Nháp)'.");
            }

            if (order.OrderDetails.Any())
            {
                _context.OrderDetails.RemoveRange(order.OrderDetails);
            }

            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> SellerApproveOrderDesignAsync(
        int orderId,
        ProductionStatus action,
        string sellerId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails.Where(od => od.NeedDesign == true))
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return false;
            if (order.SellerUserId != sellerId) throw new UnauthorizedAccessException($"Seller {sellerId} is not authorized to modify Order {orderId}.");
            if (order.StatusOrder != 5) throw new InvalidOperationException($"Order {orderId} is not in CHECK_DESIGN status (4). Current status: {order.StatusOrder}.");

            var allDetailsInCheckDesign = order.OrderDetails.All(od =>
                od.ProductionStatus.GetValueOrDefault() == ProductionStatus.CHECK_DESIGN
            );

            if (!allDetailsInCheckDesign) throw new InvalidOperationException($"Not all OrderDetails in Order {orderId} are in CHECK_DESIGN status. Cannot proceed with approval/rejection.");

            int newOrderStatus;
            ProductionStatus newProductionStatus;

            if (action == ProductionStatus.DESIGN_REDO)
            {
                newOrderStatus = 6;
                newProductionStatus = ProductionStatus.DESIGN_REDO;
            }
            else if (action == ProductionStatus.READY_PROD)
            {
                newOrderStatus = 7;
                newProductionStatus = ProductionStatus.READY_PROD;
            }
            else
            {
                throw new InvalidOperationException($"Invalid action status {action}. Must be DESIGN_REDO (3) or CONFIRMED (5).");
            }

            order.StatusOrder = newOrderStatus;
            foreach (var detail in order.OrderDetails)
            {
                detail.ProductionStatus = newProductionStatus;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> SellerApproveOrderDetailDesignAsync(
        int orderDetailId,
        ProductionStatus action,
        string sellerId)
        {
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (orderDetail == null) return false;

            var order = orderDetail.Order;

            if (order.SellerUserId != sellerId) throw new UnauthorizedAccessException($"Seller {sellerId} is not authorized to modify OrderDetail {orderDetailId}.");
            if (orderDetail.ProductionStatus.GetValueOrDefault() != ProductionStatus.CHECK_DESIGN) throw new InvalidOperationException($"OrderDetail {orderDetailId} is not in a modifiable state (Must be CHECK_DESIGN).");
            if (action != ProductionStatus.DESIGN_REDO && action != ProductionStatus.READY_PROD) throw new InvalidOperationException($"Invalid action status {action}. Must be DESIGN_REDO or READY_PROD.");

            orderDetail.ProductionStatus = action;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> SendOrderToReadyProdAsync(int orderId, string sellerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null) { await transaction.RollbackAsync(); return false; }
                if (order.SellerUserId != sellerId) throw new UnauthorizedAccessException($"Seller {sellerId} is not authorized to modify Order {orderId}.");
                if (order.StatusOrder != 1) throw new InvalidOperationException($"Chỉ có thể chuyển đơn hàng ở trạng thái 'Mới tạo' (StatusOrder = 1). Trạng thái hiện tại: {order.StatusOrder}.");

                const int READY_PROD_ORDER_STATUS = 7;
                order.StatusOrder = READY_PROD_ORDER_STATUS;

                foreach (var detail in order.OrderDetails)
                {
                    detail.ProductionStatus = ProductionStatus.READY_PROD;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đơn hàng ID {OrderId} sang READY_PROD.", orderId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ApproveOrderResult> ApproveOrderForShippingAsync(int orderId)
        {
            var order = await _context.Orders
                                    .Include(o => o.OrderDetails)
                                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return new ApproveOrderResult { IsSuccess = false, OrderFound = false };
            if (order.OrderDetails == null || !order.OrderDetails.Any()) return new ApproveOrderResult { IsSuccess = false, CanApprove = false, ErrorMessage = "Order has no product details." };

            bool allDetailsQcDone = order.OrderDetails.All(d => d.ProductionStatus == ProductionStatus.QC_DONE);

            if (!allDetailsQcDone) return new ApproveOrderResult { IsSuccess = false, CanApprove = false, ErrorMessage = "Not all products have passed QC (Status QC_DONE)." };

            order.StatusOrder = 14;

            try
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                return new ApproveOrderResult { IsSuccess = true };
            }
            catch (DbUpdateException)
            {
                return new ApproveOrderResult { IsSuccess = false, ErrorMessage = "Database error occurred while updating the order status." };
            }
            catch (Exception)
            {
                return new ApproveOrderResult { IsSuccess = false, ErrorMessage = "An unexpected error occurred." };
            }
        }
    }
}