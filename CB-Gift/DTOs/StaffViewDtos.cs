namespace CB_Gift.DTOs
{
    public class StaffViewDtos
    {
        public class StaffPlanDetailDto
        {
            public int PlanDetailId { get; set; }
            public int OrderId { get; set; }
            public string OrderCode { get; set; }

            /// Tên khách hàng (lấy từ EndCustomer).
            public string CustomerName { get; set; }

            /// URL hình ảnh sản phẩm của đơn hàng.
            public string ImageUrl { get; set; }

            /// Nội dung cần khắc/in hoặc ghi chú đặc biệt từ đơn hàng.
            public string NoteOrEngravingContent { get; set; }

            /// URL để tải file thiết kế sản xuất.
            public string ProductionFileUrl { get; set; }

            /// URL để tải file thiệp cảm ơn.
            public string ThankYouCardUrl { get; set; }
        }

        /// DTO đại diện cho một nhóm các đơn hàng được nhóm theo ngày giờ.
        /// Ví dụ: "03/10 08:00 (2 đơn)"
        public class StaffDateGroupDto
        {
            /// Ngày giờ của nhóm.
            public DateTime GroupDate { get; set; }

            /// Số lượng đơn hàng trong nhóm này.
            public int ItemCount { get; set; }

            /// Danh sách các chi tiết đơn hàng thuộc nhóm này.
            public List<StaffPlanDetailDto> Details { get; set; } = new List<StaffPlanDetailDto>();
        }

        /// DTO cấp cao nhất, đại diện cho toàn bộ dữ liệu của một Category được chọn.
        /// Đây là đối tượng chính mà API sẽ trả về.
        public class StaffCategoryPlanViewDto
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }

            /// Tổng số đơn hàng trong category này ("67 đơn hàng").
            public int TotalItems { get; set; }

            /// Danh sách các nhóm được gom theo ngày.
            public List<StaffDateGroupDto> DateGroups { get; set; } = new List<StaffDateGroupDto>();
        }
    }
}
