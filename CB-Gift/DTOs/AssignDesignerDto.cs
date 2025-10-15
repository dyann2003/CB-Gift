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
        public string ProductDescribe { get; set; }
        public string ProductTemplate { get; set; }
        public ProductDetails ProductDetails { get; set; }
        public int Quantity { get; set; }
        public string LinkImg { get; set; } // Link ảnh mẫu
        public string Note { get; set; } // Ghi chú từ Seller
        public int OrderStatus { get; set; }
        public DateTime? AssignedAt { get; set; }
    }
    public class ProductDetails
    {
        public string ProductVariantId { get; set; }
        public decimal? LengthCm { get; set; }

        public decimal? HeightCm { get; set; }

        public decimal? WidthCm { get; set; }
        public string ThicknessMm { get; set; }

        public string SizeInch { get; set; }

        public string Layer { get; set; }

        public string CustomShape { get; set; }

        public string Sku { get; set; }
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
    public class UpdateStatusRequest
    {
        // Frontend sẽ gửi JSON: { "orderStatus": 4 }
        [Required]
        public int OrderStatus { get; set; }
    }
}
