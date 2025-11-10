namespace CB_Gift.DTOs
{
    public class AiPromptDto
    {
        public string Prompt { get; set; }
        public IFormFile ImageFile { get; set; }
        public string? Style { get; set; }
        public string? AspectRatio { get; set; }
        public string? Quality { get; set; }
    }
}
