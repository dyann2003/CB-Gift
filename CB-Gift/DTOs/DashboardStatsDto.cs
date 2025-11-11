namespace CB_Gift.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalOrders { get; set; }
        public int NeedsAction { get; set; } // Cần Hành Động (Design, Production Ready)
        public int UrgentIssues { get; set; } // Sự Cố Khẩn Cấp (Làm Lại Thiết Kế, Sản xuất Lại)
        public int Completed { get; set; } // Đơn Hàng Hoàn Thành (QC Done, Shipped)
    }
}
