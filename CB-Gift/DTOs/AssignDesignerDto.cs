using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class AssignDesignerDto
    {
        [Required]
        public string SellerUserId { get; set; }

        [Required]
        public string DesignerUserId { get; set; }
    }
    public class DesignerSellerDto
    {
        public string DesignerUserId { get; set; }
        public string SellerUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUserId { get; set; }
        // Thêm các thuộc tính như Tên Designer, Tên Seller 
         public string DesignerName { get; set; } 
         public string SellerName { get; set; }
    }
    public class DesignTaskDto
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string LinkImg { get; set; } // Link ảnh mẫu
        public string Note { get; set; } // Ghi chú từ Seller
        public DateTime? AssignedAt { get; set; }
    }
    public class UploadDesignDto
    {
        [Required]
        public IFormFile DesignFile { get; set; }
        public string Note { get; set; }
    }
    public class AssignDesignerToOrderDetailDto
    {
        [Required]
        public string DesignerUserId { get; set; }
    }
}
