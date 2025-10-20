using CB_Gift.Models.Enums;

namespace CB_Gift.DTOs
{
    public class StaffViewDtos
    {
        public class StaffPlanDetailDto
        {
            public int PlanDetailId { get; set; }
            public int OrderId { get; set; }
            public string OrderCode { get; set; }

            public string CustomerName { get; set; }

            public string ImageUrl { get; set; }

            public string NoteOrEngravingContent { get; set; }

            public string ProductionFileUrl { get; set; }

            public string ThankYouCardUrl { get; set; }

            public int Quantity { get; set; }

            public ProductionStatus? StatusOrder { get; set; }
        }

        public class StaffDateGroupDto
        {
            public DateTime GroupDate { get; set; }

            public int ItemCount { get; set; }

            public List<StaffPlanDetailDto> Details { get; set; } = new List<StaffPlanDetailDto>();
        }

        public class StaffCategoryPlanViewDto
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }

            public int TotalItems { get; set; }

            public List<StaffDateGroupDto> DateGroups { get; set; } = new List<StaffDateGroupDto>();
        }
    }
}
