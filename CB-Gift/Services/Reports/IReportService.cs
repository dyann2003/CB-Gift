using CB_Gift.DTOs;
using CB_Gift.DTOs.Reports;

namespace CB_Gift.Services.Reports
{
    public interface IReportService
    {
        Task<KpiDto> GetFinancialKpisAsync(ReportFilterDto filter);
        Task<List<RevenueChartDto>> GetRevenueChartAsync(ReportFilterDto filter);
        Task<List<FinancialIssueDto>> GetFinancialIssuesChartAsync(ReportFilterDto filter);
        Task<List<ReprintReasonDto>> GetReprintReasonsChartAsync(ReportFilterDto filter);
        Task<List<TopSellerDto>> GetTopSellersAsync(ReportFilterDto filter);
        Task<IEnumerable<SellerDto>> GetAllSellersAsync();
        Task<OperationsKpiDto> GetOperationsKpisAsync(ReportFilterDto filter);
        Task<List<OrderStatusChartDto>> GetOrderStatusDistributionAsync(ReportFilterDto filter);
        Task<List<IncomingOutgoingDto>> GetIncomingOutgoingChartAsync(ReportFilterDto filter);
        Task<List<IssueBreakdownDto>> GetIssueBreakdownAsync(ReportFilterDto filter);
        Task<List<CriticalOrderDto>> GetCriticalOrdersAsync(ReportFilterDto filter);
    }
}
