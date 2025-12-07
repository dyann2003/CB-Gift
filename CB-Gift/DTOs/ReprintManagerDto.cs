using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class ReprintManagerDto
    {
        [Required]
        public List<int> OriginalOrderDetailIds { get; set; }

        /* [Required]
         public string ManagerUserId { get; set; }*/

        // Lý do Manager từ chối 
        public string? RejectReason { get; set; }
    }
    public class ReprintItemDto
    {
        public int OrderDetailId { get; set; }
        public string ProductName { get; set; }
        public string SKU { get; set; }
        public int Quantity { get; set; }
        public bool ReprintSelected { get; set; } = true; // Luôn là true cho reprint
    }
    public class ReprintDetailsDto
    {
        // Các trường chi tiết về yêu cầu
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; } // Lý do chi tiết (Đã có trong Reprint Model)

        public string? ProofUrl { get; set; } // Link bằng chứng (thay thế cho NewDesignFileUrl)

        // Đã bỏ: ReasonType, NeedsDesignChange, NewDesignFileUrl

        // Thông tin Duyệt
        public string? RejectionReason { get; set; } // Lý do từ chối (của staff/manager)
        public DateTime RequestDate { get; set; }

        // Danh sách các OrderDetail liên quan (ReprintItemDto giữ nguyên)
        public List<ReprintItemDto> RequestedItems { get; set; } = new List<ReprintItemDto>();
    }
}
