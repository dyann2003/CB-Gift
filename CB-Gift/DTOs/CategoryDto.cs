using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryCode { get; set; }
        public int? Status { get; set; }
    }
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
        public string CategoryName { get; set; }

        [StringLength(50, ErrorMessage = "Mã danh mục không được vượt quá 50 ký tự.")]
        public string CategoryCode { get; set; }

        // Mặc định trạng thái là 1 (hoạt động) khi tạo mới
        public int? Status { get; set; } = 1;
    }
    public class UpdateCategoryDto
    {
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
        public string CategoryName { get; set; }

        [StringLength(50, ErrorMessage = "Mã danh mục không được vượt quá 50 ký tự.")]
        public string CategoryCode { get; set; }

        public int? Status { get; set; }
    }
    public class UpdateCategoryStatusDto
    {
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        // thêm validation để Status chỉ nhận các giá trị hợp lệ (ví dụ: 0 hoặc 1)
        [Range(0, 1, ErrorMessage = "Trạng thái chỉ có thể là 0 (Ẩn) hoặc 1 (Hiện).")]
        public int Status { get; set; }
    }
}
