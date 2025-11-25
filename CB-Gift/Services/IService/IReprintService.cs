using CB_Gift.DTOs;

namespace CB_Gift.Services.IService
{
    public interface IReprintService
    {
        // User submit reprint request
        Task SubmitReprintAsync(ReprintSubmitDto dto);

        // Manager approve reprint
        Task ApproveReprintAsync(ReprintManagerDto dto);

        // Manager reject reprint
        Task RejectReprintAsync(ReprintManagerDto dto);
    }
}
