namespace CB_Gift.DTOs.Reports
{
    // DTO tổng trả về cho API
    public class FinancialDashboardDto
    {
        public KpiDto Kpis { get; set; }
        public List<RevenueChartDto> RevenueData { get; set; }
        public List<FinancialIssueDto> FinancialData { get; set; }
        public List<ReprintReasonDto> ReprintReasons { get; set; }
        public List<TopSellerDto> TopSellers { get; set; }
    }

    public class KpiDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal CashCollected { get; set; }
        public decimal OutstandingDebt { get; set; }
        public decimal TotalRefunds { get; set; }
        public double ReprintRate { get; set; }
    }

    public class RevenueChartDto
    {
        public string Date { get; set; }
        public decimal Invoiced { get; set; }
        public decimal Collected { get; set; }
    }

    public class FinancialIssueDto
    {
        public string Month { get; set; }
        public int Refunds { get; set; }
        public int Reprints { get; set; }
    }

    public class ReprintReasonDto
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public string Fill { get; set; } // Mã màu Hex cho biểu đồ
    }

    public class TopSellerDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Orders { get; set; }
        public decimal Revenue { get; set; }
        public decimal Debt { get; set; }
        public double IssueRate { get; set; }
    }

    public class ReportFilterDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? SellerId { get; set; }
    }
}