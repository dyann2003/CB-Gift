using System.ComponentModel.DataAnnotations;

namespace CB_Gift.DTOs
{
    public class TagDto
    {
        public int TagsId { get; set; }
        public string TagName { get; set; }
        public string TagCode { get; set; }
    }
    public class CreateTagDto
    {
        [Required(ErrorMessage = "Tên tag không được để trống.")]
        [StringLength(100)]
        public string TagName { get; set; }

        [StringLength(100)]
        public string TagCode { get; set; }
    }
    public class UpdateTagDto
    {
        [Required(ErrorMessage = "Tên tag không được để trống.")]
        [StringLength(100)]
        public string TagName { get; set; }

        [StringLength(100)]
        public string TagCode { get; set; }
    }
}
