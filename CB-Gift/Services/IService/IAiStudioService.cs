namespace CB_Gift.Services.IService
{
    public interface IAiStudioService
    {
        Task<string> GenerateImageAsync(
            string base64Image,
            string userPrompt,
            string? style,
            string? aspectRatio,
            string? quality);
    }
}
