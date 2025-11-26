using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static CB_Gift.DTOs.StaffViewDtos;

namespace CB_Gift.Services
{
    public class PlanService : IPlanService
    {
        private readonly CBGiftDbContext _context;
        private readonly ILogger<PlanService> _logger;

        public PlanService(ILogger<PlanService> logger, CBGiftDbContext context)
        {
            _logger = logger;
            _context = context ?? throw new ArgumentNullException(nameof(context));
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

            // Lọc theo category nếu có
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(pd => pd.OrderDetail.ProductVariant.Product.CategoryId == categoryId.Value);
            }

            // Lọc theo ngày tạo plan được chọn
            if (selectedDate.HasValue)
            {
                query = query.Where(pd => pd.Plan.CreateDate.HasValue && pd.Plan.CreateDate.Value.Date == selectedDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(status))
            {
                //0 là needs_production, 1 là đang sản xuất , 2 là produced
                if (status.Equals("produced", StringComparison.OrdinalIgnoreCase))
                {
                    //query = query.Where(pd => pd.StatusOrder == 2);
                    query = query.Where(pd => pd.OrderDetail.ProductionStatus == ProductionStatus.FINISHED);

                }
                else if (status.Equals("needs_production", StringComparison.OrdinalIgnoreCase))
                {
                    //query = query.Where(pd => pd.StatusOrder == 0 || pd.StatusOrder == 1);
                    query = query.Where(pd => pd.OrderDetail.ProductionStatus == ProductionStatus.READY_PROD 
                    || pd.OrderDetail.ProductionStatus == ProductionStatus.IN_PROD 
                    || pd.OrderDetail.ProductionStatus == ProductionStatus.PROD_REWORK
                    || pd.OrderDetail.ProductionStatus == ProductionStatus.QC_FAIL);
                }
                else
                {
                    Console.WriteLine("Status incorrect!");
                    return new List<StaffCategoryPlanViewDto>();
                }
            }

            // Xây dựng toàn bộ câu truy vấn và chỉ thực thi một lần duy nhất ở cuối
            var results = await query
                .GroupBy(pd => pd.OrderDetail.ProductVariant.Product.Category) // Gom nhóm cấp 1 trên DB
                .Select(categoryGroup => new StaffCategoryPlanViewDto
                {
                    CategoryId = categoryGroup.Key.CategoryId,
                    CategoryName = categoryGroup.Key.CategoryName ?? "Unknown",
                    TotalItems = categoryGroup.Count(), // DB sẽ thực hiện COUNT

                    DateGroups = categoryGroup
                        .GroupBy(pd => pd.Plan.CreateDate.Value.Date) // Gom nhóm cấp 2 trên DB
                        .OrderBy(dateGroup => dateGroup.Key)
                        .Select(dateGroup => new StaffDateGroupDto
                        {
                            GroupDate = dateGroup.Key,
                            ItemCount = dateGroup.Count(),
                            Details = dateGroup.Select(pd => new StaffPlanDetailDto
                            {
                                PlanDetailId = pd.PlanDetailId,
                                OrderDetailId = pd.OrderDetailId,
                                OrderId = pd.OrderDetail.OrderId,
                                OrderCode = pd.OrderDetail.Order.OrderCode,
                                CustomerName = pd.OrderDetail.Order.EndCustomer.Name,
                                ImageUrl = pd.OrderDetail.LinkImg,
                                NoteOrEngravingContent = pd.OrderDetail.Note,
                                ProductionFileUrl = pd.OrderDetail.LinkFileDesign,
                                ThankYouCardUrl = pd.OrderDetail.LinkThanksCard,
                                Quantity = pd.OrderDetail.Quantity,
                                StatusOrder = pd.OrderDetail.ProductionStatus,
                                Reason = (
                                        pd.OrderDetail.ProductionStatus == ProductionStatus.PROD_REWORK ||
                                        pd.OrderDetail.ProductionStatus == ProductionStatus.QC_FAIL
                                    )
                                    ? (from log in _context.OrderDetailLogs
                                       where log.OrderDetailId == pd.OrderDetailId &&
                                             log.EventType == "QC_REJECTED"
                                       orderby log.CreatedAt descending
                                       select log.Reason
                                      ).FirstOrDefault()
                                    : null
                            }).ToList()
                        }).ToList()
                })
                .ToListAsync();

            return results;
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
                ProductionStatus.CANCELLED => 17,

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
                ProductionStatus.PACKING => 13,
                ProductionStatus.FINISHED => 10, // Sản xuất xong

                // Trạng thái không rõ hoặc mặc định
                _ => 1
            };
        }
    }
}
