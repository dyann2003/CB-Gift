namespace CB_Gift.DTOs
{
    public class QcDashboardDto
    {
        public QcDashboardStatsDto Stats { get; set; }
        public IEnumerable<PendingDesignReviewDto> PendingDesignReviews { get; set; }
        public IEnumerable<PendingProductCheckDto> PendingProductChecks { get; set; }
    }
    public class QcDashboardStatsDto
    {
        public int DesignReviewCount { get; set; }
        public int ProductCheckCount { get; set; }
        public int ApprovedTodayCount { get; set; }
        public double QualityScore { get; set; }
    }

    public class PendingDesignReviewDto
    {
        public string Id { get; set; }
        public string Customer { get; set; }
        public string Product { get; set; }
        public string Designer { get; set; }
        public DateTime DateSubmitted { get; set; }
        public string Status { get; set; }
    }

    public class PendingProductCheckDto
    {
        public string Id { get; set; }
        public string Customer { get; set; }
        public string Product { get; set; }
        public string Staff { get; set; }
        public DateTime DateManufactured { get; set; }
        public string Status { get; set; }
    }
}
