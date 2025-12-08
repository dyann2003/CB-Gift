namespace CB_Gift.DTOs
{
    //public class AiPromptDto
    //{
    //    public string Prompt { get; set; }
    //    public IFormFile ImageFile { get; set; }
    //    public string? Style { get; set; }
    //    public string? AspectRatio { get; set; }
    //    public string? Quality { get; set; }
    //}
    public class AiPromptDto
    {
        public IFormFile ImageFile { get; set; }
        public string Prompt { get; set; }
        public string? Style { get; set; } // Map vào style_preset

        public string? Quality { get; set; } // Frontend gửi: "standard", "hd", ...

        // Nhận giá trị từ Slider (0.1 - 1.0)
        public double ControlStrength { get; set; } = 0.7;

        // (Optional) Giữ lại nếu dùng chung, nhưng Controller Structure sẽ bỏ qua
        public string? AspectRatio { get; set; }
    }
}
