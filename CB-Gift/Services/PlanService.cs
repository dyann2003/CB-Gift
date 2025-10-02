using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class PlanService : IPlanService
    {
        private readonly CBGiftDbContext _context;

        public PlanService(CBGiftDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// Lấy tất cả OrderDetail đã submit (StatusOrder = 2) chưa có PlanDetail
        private async Task<List<OrderDetail>> GetSubmittedOrderDetailsAsync()
        {
            return await _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Order.StatusOrder == 2
                             && !_context.PlanDetails.Any(pd => pd.OrderDetailId == od.OrderDetailId))
                .ToListAsync();
        }

        /// Tạo Plan mới
        private async Task<Plan> CreatePlanAsync(string createdUserId)
        {
            var plan = new Plan
            {
                CreateDate = DateTime.UtcNow,
                CreateByUserId = createdUserId,
                StartDatePlan = DateTime.UtcNow // giả sử start ngay khi tạo
            };

            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        /// Thêm PlanDetail cho từng OrderDetail
        private async Task AddPlanDetailAsync(Plan plan, OrderDetail orderDetail)
        {
            var planDetail = new PlanDetail
            {
                PlanId = plan.PlanId,
                OrderDetailId = orderDetail.OrderDetailId,
                StatusOrder = 0, // pending
                NumberOfFinishedProducts = 0
            };

            _context.PlanDetails.Add(planDetail);
            await _context.SaveChangesAsync();
        }

        /// Gom đơn theo ProductVariantId (mỗi variant sẽ tạo 1 Plan riêng)
        public async Task GroupSubmittedOrdersAsync(string createdUserId)
        {
            var orderDetails = await GetSubmittedOrderDetailsAsync();

            // group theo product variant
            var grouped = orderDetails.GroupBy(od => od.ProductVariantId);

            foreach (var group in grouped)
            {
                // tạo plan cho nhóm variant này
                var plan = await CreatePlanAsync(createdUserId);

                foreach (var od in group)
                {
                    await AddPlanDetailAsync(plan, od);
                }
            }
        }
    }

}
