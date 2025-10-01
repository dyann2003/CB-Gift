using Microsoft.AspNetCore.Mvc;

namespace CB_Gift.Services.IService
{
    public interface IQrCodeService
    {
        Task<object> GenerateQrCodeAsync(int orderDetailId);
        Task<IEnumerable<object>> GenerateQrCodesAsync(List<int> orderDetailIds);
    }
}
