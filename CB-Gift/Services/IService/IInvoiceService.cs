using CB_Gift.DTOs;
using CB_Gift.Models;

namespace CB_Gift.Services.IService
{
    public interface IInvoiceService
    {
        Task<Invoice> CreateInvoiceAsync(CreateInvoiceRequest request, string staffId);
        Task<string> CreatePaymentLinkAsync(CreatePaymentLinkRequest request, string sellerId);
        Task<int> LogWebhookAsync(string gateway, string payload, string signature);
        Task ProcessWebhookPaymentAsync(int webhookLogId, string gatewayName);
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
        Task<PaginatedResult<SellerReceivablesDto>> GetSellerReceivablesAsync(
         string? searchTerm,
         string? sortColumn,
         string? sortDirection,
         int page,
         int pageSize,
         // [THÊM MỚI] Các filter theo khoảng giá trị
         decimal? minDebt,
         decimal? maxDebt,
         decimal? minSales,
         decimal? maxSales
     );
        Task<PaginatedResult<SellerReceivablesDto>> GetSellerReceivables1Async(
        string? searchTerm,
        string? sortColumn,
        string? sortDirection,
        int page,
        int pageSize
        );
        // [API CHO TAB "SALES HISTORY" - LIST CÁC THÁNG]
        Task<List<SellerMonthlySalesDto>> GetSellerMonthlySalesAsync(string sellerId);

        // [API CHO TAB "SALES HISTORY" - LIST ORDER KHI MỞ RỘNG]
        Task<PaginatedResult<SellerOrderDto>> GetSellerOrdersForMonthAsync(string sellerId, int year, int month, int page, int pageSize);

        // [API CHO NÚT "CREATE MONTHLY RECEIPT"]
        Task<Invoice> CreateInvoiceForMonthAsync(CreateMonthlyInvoiceRequest request, string staffId);

        // Phương thức mới dành cho Job tự động
        Task RunMonthlyInvoiceCreationJobAsync(string staffId, int year, int month);

        // [API CHO TAB "PAYMENT HISTORY"]
        //huy
        Task<PaginatedResult<PaymentSummaryDto>> GetPaymentsForSellerAsync(string sellerId, int page, int pageSize);
    }
}
