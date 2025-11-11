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
        Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesForSellerPageAsync(
        string sellerId,
        string? status,
        string? searchTerm,
        int page,
        int pageSize
        );
        // Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<PaginatedResult<InvoiceSummaryDto>> GetAllInvoicesAsync(
        string? status,
        string? searchTerm,
        string? sellerId,
        int page,
        int pageSize
        );
        Task<OverdueCheckDto> CheckForOverdueInvoiceAsync(string sellerId);
    }
}
