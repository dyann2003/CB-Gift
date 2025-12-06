using CB_Gift.Orders.Import;

namespace CB_Gift.Services.IService
{
    public interface IOrderImportService
    {
        Task<OrderImportResult> ImportFromExcelAsync(IFormFile file, string sellerUserId);
        byte[] GenerateExcelTemplate();
    }
}
