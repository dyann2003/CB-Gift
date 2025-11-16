namespace CB_Gift.Services.IService
{
    public interface IGhnPrintService
    {
        Task<string> GetPrintUrlAsync(List<string> orderCodes, string size = "A5");
    }
}
