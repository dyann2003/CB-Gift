namespace CB_Gift.Services.IService
{
    public interface IAiStudioService
    {
        Task<string> GenerateLineArtFromChibiAsync(string base64Image);
    }
}
