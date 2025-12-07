using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static CB_Gift.DTOs.StaffViewDtos;

namespace CB_Gift.Services
{
    public class PlanService : IPlanService
    {
        private readonly CBGiftDbContext _context;
        private readonly ILogger<PlanService> _logger;
        private readonly INotificationService _notificationService;
        private readonly UserManager<AppUser> _userManager;
        public PlanService(ILogger<PlanService> logger, CBGiftDbContext context, INotificationService notificationService, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _notificationService = notificationService;
            _userManager = userManager;
        }

        /// Lấy tất cả OrderDetail đã submit (StatusOrder = 7) chưa có PlanDetail
        private async Task<List<OrderDetail>> GetSubmittedOrderDetailsAsync()
        {
            return await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Where(od => od.Order.StatusOrder == 7
                             && !_context.PlanDetails.Any(pd => pd.OrderDetailId == od.OrderDetailId))
                .ToListAsync();
        }

        /// Gom đơn theo ProductVariantId (mỗi variant sẽ tạo 1 Plan riêng)
        public async Task GroupSubmittedOrdersAsync(string createdUserId)
        {
            // 1. Ghi log thời gian bắt đầu chạy
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Job GroupSubmittedOrdersAsync STARTED at: {Time}", startTime);

            try
            {
                var orderDetailsToGroup = await GetSubmittedOrderDetailsAsync();

                if (!orderDetailsToGroup.Any())
                {
                    _logger.LogInformation("Job GroupSubmittedOrdersAsync SKIPPED. No orders to group.");
                    return;
                }

                var groupedByCategory = orderDetailsToGroup.GroupBy(od => od.ProductVariant.Product.CategoryId);
                var ordersToUpdate = new HashSet<Order>();

                int plansCreated = 0;

                foreach (var group in groupedByCategory)
                {
                    var newPlan = new Plan
                    {
                        CreateDate = DateTime.UtcNow,
                        CreateByUserId = createdUserId,
                        StartDatePlan = DateTime.UtcNow
                    };
                    _context.Plans.Add(newPlan);
                    plansCreated++;

                    foreach (var orderDetail in group)
                    {
                        var newPlanDetail = new PlanDetail
                        {
                            Plan = newPlan,
                            OrderDetailId = orderDetail.OrderDetailId,
                            StatusOrder = 0,
                            NumberOfFinishedProducts = 0
                        };
                        _context.PlanDetails.Add(newPlanDetail);

                        if (orderDetail.Order != null)
                        {
                            ordersToUpdate.Add(orderDetail.Order);
                        }
                    }
                }

                foreach (var order in ordersToUpdate)
                {
                    order.StatusOrder = 8;
                    var orderDetails = await _context.OrderDetails
                        .Where(od => od.OrderId == order.OrderId)
                        .ToListAsync();

                    foreach (var detail in orderDetails)
                    {
                        detail.ProductionStatus = ProductionStatus.READY_PROD;
                    }
                }

                await _context.SaveChangesAsync();

                // 2. Ghi log khi hoàn thành thành công
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Job GroupSubmittedOrdersAsync COMPLETED successfully. Created {PlanCount} plans. Processed {OrderCount} orders. Duration: {Duration}ms",
                    plansCreated, ordersToUpdate.Count, duration.TotalMilliseconds);

                // 3. GỬI THÔNG BÁO CHO STAFF
                try
                {
                    if (plansCreated > 0)
                    {
                        var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
                        var staffIds = staffUsers.Select(u => u.Id).ToList();

                        if (staffIds.Any())
                        {
                            string message = $"Hệ thống vừa tạo {plansCreated} kế hoạch sản xuất mới. Vui lòng kiểm tra.";
                            string link = "/staff/needs-production";

                            foreach (var staffId in staffIds)
                            {
                                await _notificationService.CreateAndSendNotificationAsync(
                                    staffId.ToString(),
                                    message,
                                    link
                                );
                            }

                            _logger.LogInformation("Sent notifications to {Count} staff members.", staffIds.Count);
                        }
                    }
                }
                catch (Exception exNotify)
                {
                    _logger.LogError(exNotify, "Error sending notifications to Staff.");
                }
            }
            catch (Exception ex)
            {
                // 3. Ghi log khi có lỗi (Quan trọng nhất)
                _logger.LogError(ex, "Job GroupSubmittedOrdersAsync FAILED at: {Time}", DateTime.UtcNow);
            }
        }

        public async Task<IEnumerable<StaffCategoryPlanViewDto>> GetPlansForStaffViewAsync(int? categoryId, DateTime? selectedDate, string status)
        {
            var query = _context.PlanDetails.AsNoTracking();

            // --- 1. ÁP DỤNG CÁC BỘ LỌC (FILTER) ---
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(pd => pd.OrderDetail.ProductVariant.Product.CategoryId == categoryId.Value);
            }

            if (selectedDate.HasValue)
            {
                // Lọc theo ngày (Bỏ qua phần giờ phút giây)
                query = query.Where(pd => pd.Plan.CreateDate.HasValue && pd.Plan.CreateDate.Value.Date == selectedDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status.Equals("produced", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(pd => pd.OrderDetail.ProductionStatus == ProductionStatus.FINISHED);
                }
                else if (status.Equals("needs_production", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(pd => pd.OrderDetail.ProductionStatus == ProductionStatus.READY_PROD
                                           || pd.OrderDetail.ProductionStatus == ProductionStatus.IN_PROD
                                           || pd.OrderDetail.ProductionStatus == ProductionStatus.QC_FAIL
                                           || pd.OrderDetail.ProductionStatus == ProductionStatus.PROD_REWORK);
                }
                else
                {
                    Console.WriteLine("Status incorrect!");
                    return new List<StaffCategoryPlanViewDto>();
                }
            }

            // --- 2. LẤY DỮ LIỆU PHẲNG (FLATTEN DATA) ---
            // Select ra một danh sách phẳng chứa tất cả thông tin cần thiết.
            // EF Core sẽ dịch đoạn này thành 1 câu SQL tối ưu, bao gồm cả Sub-query lấy Reason.
            var flatData = await query.Select(pd => new
            {
                // Grouping Keys (Dùng để gom nhóm sau này)
                CategoryId = pd.OrderDetail.ProductVariant.Product.CategoryId,
                CategoryName = pd.OrderDetail.ProductVariant.Product.Category.CategoryName,
                PlanDate = pd.Plan.CreateDate,
                OrderId = pd.OrderDetail.OrderId,
                OrderCode = pd.OrderDetail.Order.OrderCode,
                CustomerName = pd.OrderDetail.Order.EndCustomer.Name,

                // Data Details
                PlanDetailId = pd.PlanDetailId,
                OrderDetailId = pd.OrderDetailId,
                ImageUrl = pd.OrderDetail.LinkImg,
                Note = pd.OrderDetail.Note,
                ProductionFileUrl = pd.OrderDetail.LinkFileDesign,
                ThankYouCardUrl = pd.OrderDetail.LinkThanksCard,
                Quantity = pd.OrderDetail.Quantity,
                StatusOrder = pd.OrderDetail.ProductionStatus,
                Sku = pd.OrderDetail.ProductVariant.Sku,
                ProductName = pd.OrderDetail.ProductVariant.Product.ProductName,

                // LOGIC LẤY REASON ĐƯỢC XỬ LÝ TẠI ĐÂY
                // EF Core hỗ trợ dịch sub-query này trong Select đơn giản
                Reason = (pd.OrderDetail.ProductionStatus == ProductionStatus.PROD_REWORK ||
                          pd.OrderDetail.ProductionStatus == ProductionStatus.QC_FAIL)
                          ? _context.OrderDetailLogs
                              .Where(log => log.OrderDetailId == pd.OrderDetailId && log.EventType == "QC_REJECTED")
                              .OrderByDescending(log => log.CreatedAt)
                              .Select(log => log.Reason)
                              .FirstOrDefault()
                          : null
            }).ToListAsync(); // <--- THỰC THI QUERY TẠI ĐÂY (Lấy dữ liệu về RAM)


            // --- 3. GOM NHÓM DỮ LIỆU TRÊN RAM (IN-MEMORY GROUPING) ---
            // Sử dụng LINQ to Objects, không liên quan đến Database nữa nên cực nhanh và không lỗi.
            var result = flatData
                .GroupBy(x => new { x.CategoryId, x.CategoryName }) // Cấp 1: Category
                .Select(catGroup => new StaffCategoryPlanViewDto
                {
                    CategoryId = catGroup.Key.CategoryId,
                    CategoryName = catGroup.Key.CategoryName ?? "Unknown",
                    TotalItems = catGroup.Count(),

                    DateGroups = catGroup
                        .Where(x => x.PlanDate.HasValue) // An toàn null
                        .GroupBy(x => x.PlanDate.Value.Date) // Cấp 2: Date
                        .OrderBy(d => d.Key)
                        .Select(dateGroup => new StaffDateGroupDto
                        {
                            GroupDate = dateGroup.Key,
                            ItemCount = dateGroup.Count(),

                            OrderGroups = dateGroup
                                .GroupBy(x => new { x.OrderId, x.OrderCode, x.CustomerName }) // Cấp 3: Order
                                .Select(orderGroup => new StaffOrderGroupDto
                                {
                                    OrderId = orderGroup.Key.OrderId,
                                    OrderCode = orderGroup.Key.OrderCode,
                                    CustomerName = orderGroup.Key.CustomerName,

                                    // Map chi tiết
                                    Details = orderGroup.Select(d => new StaffPlanDetailDto
                                    {
                                        PlanDetailId = d.PlanDetailId,
                                        OrderDetailId = d.OrderDetailId,
                                        OrderId = d.OrderId,
                                        OrderCode = d.OrderCode,
                                        CustomerName = d.CustomerName,
                                        ImageUrl = d.ImageUrl,
                                        NoteOrEngravingContent = d.Note,
                                        ProductionFileUrl = d.ProductionFileUrl,
                                        ThankYouCardUrl = d.ThankYouCardUrl,
                                        Quantity = d.Quantity,
                                        StatusOrder = d.StatusOrder,
                                        Sku = d.Sku,
                                        ProductName = d.ProductName,
                                        Reason = d.Reason // Dữ liệu đã lấy ở bước 2
                                    }).ToList()
                                }).ToList()
                        }).ToList()
                }).ToList();

            return result;
        }

        /*public async Task<bool> UpdatePlanDetailStatusAsync(int planDetailId, int newStatus)
        {
            var planDetail = await _context.PlanDetails.FindAsync(planDetailId);

            if (planDetail == null)
            {
                return false;
            }

            var orderDetail = await _context.OrderDetails.FindAsync(planDetail.OrderDetailId);

            if (orderDetail == null)
            {
                return false;
            }

            orderDetail.ProductionStatus = (ProductionStatus)newStatus;

            await _context.SaveChangesAsync();

            return true;
        }*/
        // Trong Service class của bạn (ví dụ: PlanService)

        public async Task<bool> UpdatePlanDetailStatusAsync(int planDetailId, int newStatus)
        {
            IDbContextTransaction? transaction = null;

            // Chỉ mở transaction khi provider là relational (SQL Server/SQLite/…)
            if (_context.Database.IsRelational())
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                var planDetail = await _context.PlanDetails.FindAsync(planDetailId);
                if (planDetail == null) return false;

                var orderDetail = await _context.OrderDetails.FindAsync(planDetail.OrderDetailId);
                if (orderDetail == null) return false;

                orderDetail.ProductionStatus = (ProductionStatus)newStatus;

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderId == orderDetail.OrderId);
                if (order == null) return false;

                var allOrderDetails = order.OrderDetails.ToList();
                if (!allOrderDetails.Any())
                {
                    if (transaction != null) await transaction.CommitAsync();
                    return true;
                }

                var minProductionStatusValue = allOrderDetails
                    .Min(od => (int)od.ProductionStatus.GetValueOrDefault((ProductionStatus)0));
                var minProductionStatus = (ProductionStatus)minProductionStatusValue;

                order.StatusOrder = MapProductionStatusToOrderStatus(minProductionStatus);

                await _context.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
                return true;
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
        }


        // Giữ nguyên hàm Mapping của bạn
        private int MapProductionStatusToOrderStatus(ProductionStatus productionStatus)
        {
            // Ánh xạ ProductionStatus (OrderDetail) sang StatusOrder (Order)
            return productionStatus switch
            {
                // Trạng thái chung:
                ProductionStatus.DRAFT => 1,
                ProductionStatus.CREATED => 2,
                ProductionStatus.HOLD => 16,
                ProductionStatus.HOLD_RP => 17,
                ProductionStatus.REFUND => 18,

                // Trạng thái Design:
                ProductionStatus.NEED_DESIGN => 3,
                ProductionStatus.DESIGNING => 4,
                ProductionStatus.CHECK_DESIGN => 5,
                ProductionStatus.DESIGN_REDO => 6,

                // Trạng thái Chốt Đơn/Sản xuất:
                // READY_PROD là trạng thái Sẵn sàng Sản xuất.
                ProductionStatus.READY_PROD => 8,

                // Trạng thái Sản xuất:
                ProductionStatus.IN_PROD => 8,
                ProductionStatus.PROD_REWORK => 15,
                ProductionStatus.QC_FAIL => 12, // Lỗi Sản xuất (QC_FAIL)

                // Trạng thái Hoàn thành/Giao hàng:
                ProductionStatus.QC_DONE => 11, // Đã Kiểm tra Chất lượng
                ProductionStatus.SHIPPING => 13,
                ProductionStatus.SHIPPED => 14,
                ProductionStatus.FINISHED => 10, // Sản xuất xong

                // Trạng thái không rõ hoặc mặc định
                _ => 1
            };
        }
    }
}
