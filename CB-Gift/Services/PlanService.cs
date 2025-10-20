using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;
using static CB_Gift.DTOs.StaffViewDtos;

namespace CB_Gift.Services
{
    public class PlanService : IPlanService
    {
        private readonly CBGiftDbContext _context;

        public PlanService(CBGiftDbContext context)
        {
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
            var orderDetailsToGroup = await GetSubmittedOrderDetailsAsync();

            if (!orderDetailsToGroup.Any())
            {
                return;
            }

            // Gom đơn theo category 
            var groupedByCategory = orderDetailsToGroup.GroupBy(od => od.ProductVariant.Product.CategoryId);

            // Dùng một HashSet để lưu các Order cần cập nhật, tránh việc cập nhật trùng lặp
            var ordersToUpdate = new HashSet<Order>();

            foreach (var group in groupedByCategory)
            {
                // Tạo một Plan mới cho mỗi nhóm sản phẩm
                var newPlan = new Plan
                {
                    CreateDate = DateTime.UtcNow,
                    CreateByUserId = createdUserId,
                    StartDatePlan = DateTime.UtcNow // Giả sử bắt đầu ngay khi tạo
                };
                _context.Plans.Add(newPlan);

                // Với mỗi OrderDetail trong nhóm, tạo một PlanDetail tương ứng
                foreach (var orderDetail in group)
                {
                    var newPlanDetail = new PlanDetail
                    {
                        Plan = newPlan,
                        OrderDetailId = orderDetail.OrderDetailId,
                        StatusOrder = 0, // Trạng thái ban đầu: pending
                        NumberOfFinishedProducts = 0
                    };
                    _context.PlanDetails.Add(newPlanDetail);

                    // Thêm Order cha của OrderDetail này vào danh sách cần cập nhật
                    if (orderDetail.Order != null)
                    {
                        ordersToUpdate.Add(orderDetail.Order);
                    }
                }
            }

            // Cập nhật trạng thái cho tất cả các Order đã được gom
            foreach (var order in ordersToUpdate)
            {
                order.StatusOrder = 8; // Chuyển trạng thái sang "Đã gom đơn"
            }

            await _context.SaveChangesAsync();
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
                    query = query.Where(pd => pd.StatusOrder == 2);
                }
                else if (status.Equals("needs_production", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(pd => pd.StatusOrder == 0 || pd.StatusOrder == 1);
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
                                OrderId = pd.OrderDetail.OrderId,
                                OrderCode = pd.OrderDetail.Order.OrderCode,
                                CustomerName = pd.OrderDetail.Order.EndCustomer.Name,
                                ImageUrl = pd.OrderDetail.LinkImg,
                                NoteOrEngravingContent = pd.OrderDetail.Note,
                                ProductionFileUrl = pd.OrderDetail.LinkFileDesign,
                                ThankYouCardUrl = pd.OrderDetail.LinkThanksCard,
                                Quantity = pd.OrderDetail.Quantity,
                                StatusOrder = pd.StatusOrder
                            }).ToList()
                        }).ToList()
                })
                .ToListAsync();

            return results;
        }

        public async Task<bool> UpdatePlanDetailStatusAsync(int planDetailId, int newStatus)
        {
            var planDetail = await _context.PlanDetails.FindAsync(planDetailId);

            if (planDetail == null)
            {
                return false;
            }

            planDetail.StatusOrder = newStatus;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
