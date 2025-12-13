using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.DTOs.Reports;
using CB_Gift.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CB_Gift.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly CBGiftDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        public ReportService(CBGiftDbContext context, UserManager<AppUser> userManager) 
        {
            _context = context;
            _userManager = userManager;
        }


        // --- HELPER: Tạo Query chung để không lặp code ---
        private (IQueryable<Invoice>, IQueryable<Payment>, IQueryable<Refund>, IQueryable<Order>) PrepareQueries(ReportFilterDto filter)
        {
            var fromDate = filter.StartDate.Date;
            var toDate = filter.EndDate.Date.AddDays(1).AddTicks(-1);

            var invoiceQuery = _context.Invoices.AsNoTracking().Where(i => i.CreatedAt >= fromDate && i.CreatedAt <= toDate && i.Status != "Cancelled");
            var paymentQuery = _context.Payments.AsNoTracking().Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate && p.Status == "Completed");
            var refundQuery = _context.Refunds.AsNoTracking().Where(r => r.CreatedAt >= fromDate && r.CreatedAt <= toDate && r.Status == "Approved");
            var orderQuery = _context.Orders.AsNoTracking().Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate);

            if (!string.IsNullOrEmpty(filter.SellerId) && filter.SellerId != "all")
            {
                invoiceQuery = invoiceQuery.Where(i => i.SellerUserId == filter.SellerId);
                paymentQuery = paymentQuery.Where(p => p.Invoice.SellerUserId == filter.SellerId);
                refundQuery = refundQuery.Where(r => r.RequestedBySellerId == filter.SellerId);
                orderQuery = orderQuery.Where(o => o.SellerUserId == filter.SellerId);
            }

            return (invoiceQuery, paymentQuery, refundQuery, orderQuery);
        }

        // 1. API KPI (Load siêu nhanh)
        public async Task<KpiDto> GetFinancialKpisAsync(ReportFilterDto filter)
        {
            var (invoiceQ, paymentQ, refundQ, orderQ) = PrepareQueries(filter);

            var totalRevenue = await invoiceQ.SumAsync(i => i.TotalAmount);
            var cashCollected = await paymentQ.SumAsync(p => p.Amount);
            var totalRefunds = await refundQ.SumAsync(r => r.Amount);

            var totalOrders = await orderQ.CountAsync();
            var reprintCount = await orderQ.CountAsync(o => o.StatusOrder == 11 || o.ActiveTts == true);
            var reprintRate = totalOrders > 0 ? Math.Round(((double)reprintCount / totalOrders) * 100, 1) : 0;

            return new KpiDto
            {
                TotalRevenue = totalRevenue,
                CashCollected = cashCollected,
                OutstandingDebt = Math.Max(0, totalRevenue - cashCollected),
                TotalRefunds = totalRefunds,
                ReprintRate = reprintRate
            };
        }

        // 2. API Revenue Chart
        public async Task<List<RevenueChartDto>> GetRevenueChartAsync(ReportFilterDto filter)
        {
            var (invoiceQ, paymentQ, _, _) = PrepareQueries(filter);

            var invoices = await invoiceQ.GroupBy(i => i.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.TotalAmount) }).ToListAsync();
            var payments = await paymentQ.GroupBy(p => p.PaymentDate.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(x => x.Amount) }).ToListAsync();

            var result = new List<RevenueChartDto>();
            for (var day = filter.StartDate.Date; day <= filter.EndDate.Date; day = day.AddDays(1))
            {
                result.Add(new RevenueChartDto
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    Invoiced = invoices.FirstOrDefault(x => x.Date == day)?.Amount ?? 0,
                    Collected = payments.FirstOrDefault(x => x.Date == day)?.Amount ?? 0
                });
            }
            return result;
        }

        // 3. API Financial Issues (Refund vs Reprint theo tháng)
        public async Task<List<FinancialIssueDto>> GetFinancialIssuesChartAsync(ReportFilterDto filter)
        {
            var (_, _, refundQ, _) = PrepareQueries(filter);
            // Logic group by Month đơn giản
            var data = await refundQ.GroupBy(r => r.CreatedAt.Month)
                .Select(g => new FinancialIssueDto
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key),
                    Refunds = g.Count(),
                    Reprints = 0 // TODO: Cần query thêm bảng Reprint
                }).ToListAsync();

            if (!data.Any()) data.Add(new FinancialIssueDto { Month = "Current", Refunds = 0, Reprints = 0 });
            return data;
        }

        // 4. API Reprint Reasons (Pie Chart)
        public async Task<List<ReprintReasonDto>> GetReprintReasonsChartAsync(ReportFilterDto filter)
        {
            var (_, _, refundQ, _) = PrepareQueries(filter);
            // Giả sử lấy lý do từ Refund (hoặc bảng Issue riêng)
            var raw = await refundQ.GroupBy(r => r.Reason)
                .Select(g => new { Name = g.Key, Value = g.Count() })
                .OrderByDescending(x => x.Value).Take(5).ToListAsync();

            string[] colors = { "#ef4444", "#f97316", "#eab308", "#22c55e", "#3b82f6" };
            return raw.Select((r, i) => new ReprintReasonDto
            {
                Name = r.Name.Length > 15 ? r.Name[..15] + "..." : r.Name,
                Value = r.Value,
                Fill = colors[i % colors.Length]
            }).ToList();
        }

        // 5. API Top Sellers (Nặng nhất)
        public async Task<List<TopSellerDto>> GetTopSellersAsync(ReportFilterDto filter)
        {
            var (invoiceQ, _, _, _) = PrepareQueries(filter);

            var rawData = await invoiceQ.GroupBy(i => i.SellerUserId)
                .Select(g => new {
                    Id = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount),
                    Paid = g.Sum(x => x.AmountPaid),
                    Orders = g.Count()
                })
                .OrderByDescending(x => x.Revenue).Take(5).ToListAsync();

            // Lấy tên seller
            var ids = rawData.Select(x => x.Id).ToList();
            var names = await _context.Users.Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName);

            return rawData.Select(x => new TopSellerDto
            {
                Id = x.Id,
                Name = names.ContainsKey(x.Id) ? names[x.Id] : "Unknown",
                Revenue = x.Revenue,
                Debt = x.Revenue - x.Paid,
                Orders = x.Orders,
                IssueRate = 0
            }).ToList();
        }
        public async Task<IEnumerable<SellerDto>> GetAllSellersAsync()
        {
            // 1. Lấy tất cả user có role Seller
            var sellers = await _userManager.GetUsersInRoleAsync("Seller");

            // 2. Lọc lấy user đang active và map sang DTO
            return sellers
                .Where(user => user.IsActive) // <--- THÊM DÒNG NÀY
                .Select(user => new SellerDto
                {
                    SellerId = user.Id,
                    SellerName = user.FullName
                })
                .ToList();
        }
        // --- HELPER: Chuẩn bị Query (DRY - Don't Repeat Yourself) ---
        private IQueryable<Order> PrepareOrderQuery(ReportFilterDto filter)
        {
            var fromDate = filter.StartDate.Date;
            var toDate = filter.EndDate.Date.AddDays(1).AddTicks(-1);

            var query = _context.Orders.AsNoTracking()
                .Where(o => o.CreationDate >= fromDate && o.CreationDate <= toDate);

            if (!string.IsNullOrEmpty(filter.SellerId) && filter.SellerId != "all")
            {
                query = query.Where(o => o.SellerUserId == filter.SellerId);
            }
            return query;
        }

        // API 1: KPIs (Total, Velocity, Backlog...)
        public async Task<OperationsKpiDto> GetOperationsKpisAsync(ReportFilterDto filter)
        {
            var query = PrepareOrderQuery(filter);

            var totalOrders = await query
             .Where(x => x.StatusOrder != 1) // Chỉ lấy những đơn có status khác 1
             .CountAsync();

            // Backlog: Đơn đang xử lý (Status từ 2 đến 7: NEED_DESIGN -> IN_PROD)
            // Giả sử ID status khớp với PRODUCTION_STATUS_MAP bạn gửi

            var backlog = await query.CountAsync(o => o.StatusOrder >= 2 && o.StatusOrder <= 12);

            // Velocity: Đơn Shipped / Số ngày
            var shippedCount = await query.CountAsync(o => o.StatusOrder == 14 || o.StatusOrderNavigation.Code == "SHIPPED"); // 8 = FINISHED/SHIPPED
            var days = (filter.EndDate - filter.StartDate).TotalDays + 1;
            var velocity = days > 0 ? Math.Round(shippedCount / days, 1) : 0;

            return new OperationsKpiDto
            {
                TotalOrders = totalOrders,
                Backlog = backlog,
                ProductionVelocity = velocity,
                AvgFulfillmentTime = 3.5 // Tạm hard-code hoặc cần tính toán phức tạp từ Log
            };
        }

        // API 2: Status Distribution (Bar Chart)
        public async Task<List<OrderStatusChartDto>> GetOrderStatusDistributionAsync(ReportFilterDto filter)
        {
            var query = PrepareOrderQuery(filter);

            var data = await query
                .GroupBy(o => o.StatusOrderNavigation.Code) // Group theo Code (CREATED, IN_PROD...)
                .Select(g => new OrderStatusChartDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return data;
        }

        // API 3: Incoming vs Outgoing (Line Chart)
        public async Task<List<IncomingOutgoingDto>> GetIncomingOutgoingChartAsync(ReportFilterDto filter)
        {
            // Incoming: Dựa trên CreationDate (Lấy từ PrepareOrderQuery)
            var incomingQuery = PrepareOrderQuery(filter);
            var incomingData = await incomingQuery
                .GroupBy(o => o.CreationDate.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            // Outgoing: Dựa trên OrderDate (hoặc ngày Ship). Cần query riêng vì mốc thời gian khác
            var fromDate = filter.StartDate.Date;
            var toDate = filter.EndDate.Date.AddDays(1).AddTicks(-1);

            var outgoingQuery = _context.Orders.AsNoTracking()
                .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate
                         && (o.StatusOrder == 8 || o.StatusOrderNavigation.Code == "SHIPPED")); // Status Shipped

            if (!string.IsNullOrEmpty(filter.SellerId) && filter.SellerId != "all")
                outgoingQuery = outgoingQuery.Where(o => o.SellerUserId == filter.SellerId);

            var outgoingData = await outgoingQuery
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            // Merge dữ liệu
            var result = new List<IncomingOutgoingDto>();
            for (var day = filter.StartDate.Date; day <= filter.EndDate.Date; day = day.AddDays(1))
            {
                result.Add(new IncomingOutgoingDto
                {
                    Date = day.ToString("MM/dd"),
                    Incoming = incomingData.FirstOrDefault(x => x.Date == day)?.Count ?? 0,
                    Outgoing = outgoingData.FirstOrDefault(x => x.Date == day)?.Count ?? 0
                });
            }
            return result;
        }

        // API 4: Issue Breakdown (Pie Chart)
        public async Task<List<IssueBreakdownDto>> GetIssueBreakdownAsync(ReportFilterDto filter)
        {
            // Kết hợp Refund Reason và Order QC_FAIL/DESIGN_REDO
            // 1. Lấy Refund Reason
            var fromDate = filter.StartDate.Date;
            var toDate = filter.EndDate.Date.AddDays(1).AddTicks(-1);

            var refundQuery = _context.Refunds.AsNoTracking()
                .Where(r => r.CreatedAt >= fromDate && r.CreatedAt <= toDate);

            if (!string.IsNullOrEmpty(filter.SellerId) && filter.SellerId != "all")
                refundQuery = refundQuery.Where(r => r.RequestedBySellerId == filter.SellerId);

            var refundStats = await refundQuery.GroupBy(r => r.Reason)
                .Select(g => new IssueBreakdownDto { Name = "Refund: " + g.Key, Value = g.Count() })
                .ToListAsync();

            // 2. Lấy QC Fail / Design Redo từ Order
            var orderQuery = PrepareOrderQuery(filter);
            var qcFailCount = await orderQuery.CountAsync(o => o.StatusOrderNavigation.Code == "QC_FAIL");
            var redoCount = await orderQuery.CountAsync(o => o.StatusOrderNavigation.Code == "DESIGN_REDO");

            if (qcFailCount > 0) refundStats.Add(new IssueBreakdownDto { Name = "QC Failed", Value = qcFailCount });
            if (redoCount > 0) refundStats.Add(new IssueBreakdownDto { Name = "Design Redo", Value = redoCount });

            return refundStats;
        }

        // API 5: Critical Alerts (Stuck Orders)
        public async Task<List<CriticalOrderDto>> GetCriticalOrdersAsync(ReportFilterDto filter)
        {
            // Lấy các đơn chưa hoàn thành
            var query = PrepareOrderQuery(filter)
                .Where(o => o.StatusOrder != 8 && o.StatusOrder != 14) // Chưa Ship và chưa Cancel
                .Include(o => o.SellerUser)
                .Include(o => o.StatusOrderNavigation);

            // Logic "Stuck": CreationDate quá 5 ngày mà chưa xong (Logic đơn giản vì chưa có bảng Log lịch sử)
            var stuckDate = DateTime.UtcNow.AddDays(-5);

            var data = await query
                .Where(o => o.CreationDate <= stuckDate)
                .OrderBy(o => o.CreationDate)
                .Take(10)
                .Select(o => new CriticalOrderDto
                {
                    Id = o.OrderCode,
                    Status = o.StatusOrderNavigation.Code,
                    // Tính số ngày bị kẹt (Tạm tính từ lúc tạo đơn)
                    DaysInStatus = (int)(DateTime.UtcNow - (o.CreationDate ?? DateTime.UtcNow)).TotalDays,
                    Seller = o.SellerUser.FullName ?? "Unknown"
                })
                .ToListAsync();

            return data;
        }
    }
}
