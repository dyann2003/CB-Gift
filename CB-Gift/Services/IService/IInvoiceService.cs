using CB_Gift.DTOs;
using CB_Gift.Models;

namespace CB_Gift.Services.IService
{
    public interface IInvoiceService
    {
        Task<Invoice> CreateInvoiceAsync(CreateInvoiceRequest request, string staffId);
        Task<string> CreatePaymentLinkAsync(CreatePaymentLinkRequest request, string sellerId);
        Task<int> LogWebhookAsync(string gateway, string payload, string signature);
        Task ProcessPayOSWebhookAsync(int webhookLogId);
        Task<Invoice> GetInvoiceDetailsAsync(int invoiceId);
        Task<IEnumerable<Invoice>> GetInvoicesForSellerAsync(string sellerId);
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
    }
}
