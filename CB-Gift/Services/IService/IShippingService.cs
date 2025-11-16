using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IShippingService
    {
        Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request);
        Task<TrackingResult> TrackOrderAsync(string orderCode);
    }
}
