namespace CB_Gift.Services.IService
{
    public interface IAiStudioService
    {
        //Task<string> GenerateImageAsync(
        //    string base64Image,
        //    string userPrompt,
        //    string? style,
        //    string? aspectRatio,
        //    string? quality);
        Task<string> GenerateStructureImageAsync(
            string base64Image,
            string userPrompt,
            string? stylePreset = "photographic",
            double controlStrength = 0.7,
            long? seed = 0,             
            string outputFormat = "png"
        );
    }
}
