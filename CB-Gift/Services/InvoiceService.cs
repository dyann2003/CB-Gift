using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;

namespace CB_Gift.Services;

public class InvoiceService : IInvoiceService
{
    private readonly CBGiftDbContext _context;
    private readonly PayOS _payOS;

    public InvoiceService(CBGiftDbContext context, IConfiguration configuration)
    {
        _context = context;
        _payOS = new PayOS(
            configuration["PayOS:ClientId"],
            configuration["PayOS:ApiKey"],
            configuration["PayOS:ChecksumKey"]
        );
    }

    public async Task<Invoice> CreateInvoiceAsync(CreateInvoiceRequest request, string staffId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var uninvoicedOrders = await _context.Orders
                .Where(o => o.SellerUserId == request.SellerId &&
                            o.OrderDate >= request.StartDate &&
                            o.OrderDate <= request.EndDate &&
                            !_context.InvoiceItems.Any(ii => ii.OrderId == o.OrderId))
                .ToListAsync();

            if (!uninvoicedOrders.Any())
                throw new InvalidOperationException("Không có đơn hàng mới để tạo hóa đơn.");

            var subtotal = uninvoicedOrders.Sum(o => o.TotalCost ?? 0);

            var newInvoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                SellerUserId = request.SellerId,
                CreatedByStaffId = staffId,
                InvoicePeriodStart = request.StartDate,
                InvoicePeriodEnd = request.EndDate,
                DueDate = request.EndDate.AddDays(15), 
                Subtotal = subtotal,
                TotalAmount = subtotal, // Giả sử không có thuế
                Status = "Issued",
                Notes = request.Notes
            };

            _context.Invoices.Add(newInvoice);
            await _context.SaveChangesAsync(); // Lưu để có InvoiceId

            var invoiceItems = uninvoicedOrders.Select(order => new InvoiceItem
            {
                InvoiceId = newInvoice.InvoiceId,
                OrderId = order.OrderId,
                Description = $"Order {order.OrderCode}",
                Amount = order.TotalCost ?? 0
            }).ToList();
            _context.InvoiceItems.AddRange(invoiceItems);

            _context.InvoiceHistories.Add(new InvoiceHistory { InvoiceId = newInvoice.InvoiceId, Action = "Invoice Created", UserId = staffId });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return newInvoice;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> CreatePaymentLinkAsync(CreatePaymentLinkRequest request, string sellerId)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == request.InvoiceId && i.SellerUserId == sellerId);
        if (invoice == null)
            throw new KeyNotFoundException("Không tìm thấy hóa đơn hoặc bạn không có quyền truy cập.");

        if (invoice.Status == "Paid")
            throw new InvalidOperationException("Hóa đơn này đã được thanh toán.");

        // THAY ĐỔI 1: Tạo danh sách các mặt hàng (items) theo yêu cầu của constructor mới
        var items = new List<ItemData>
        {
            new ItemData(
                name: $"Thanh toán hóa đơn {invoice.InvoiceNumber}",
                quantity: 1,
                price: (int)invoice.TotalAmount
            )
        };

        var paymentData = new PaymentData(
            orderCode: invoice.InvoiceId,
            amount: (int)invoice.TotalAmount,
            description: $"Pay Inv: {invoice.InvoiceNumber}".Substring(0, Math.Min(25, $"Pay Inv: {invoice.InvoiceNumber}".Length)),
            items: items, // <-- Truyền danh sách items vào
            cancelUrl: request.CancelUrl,
            returnUrl: request.ReturnUrl
        );

        CreatePaymentResult result = await _payOS.createPaymentLink(paymentData);
        invoice.PaymentLink = result.checkoutUrl;
        await _context.SaveChangesAsync();
        return result.checkoutUrl;
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một hóa đơn, bao gồm các mục con (đơn hàng), 
    /// lịch sử thanh toán và lịch sử thay đổi.
    /// </summary>
    public async Task<Invoice> GetInvoiceDetailsAsync(int invoiceId)
    {
        var invoice = await _context.Invoices
            // Tải kèm thông tin người bán (Seller) và người tạo (Staff) để lấy tên
            .Include(i => i.SellerUser)
            .Include(i => i.CreatedByStaff)

            // Tải kèm danh sách các mục trong hóa đơn (InvoiceItems)
            .Include(i => i.Items)
                // Với mỗi mục, tải kèm thông tin của đơn hàng (Order) tương ứng
                .ThenInclude(item => item.Order)

            // Tải kèm danh sách các lần thanh toán đã thực hiện
            .Include(i => i.Payments)

            // Tải kèm lịch sử thay đổi của hóa đơn
            .Include(i => i.History)

            // Dùng AsNoTracking để tăng hiệu suất cho các truy vấn chỉ đọc
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        return invoice; // Sẽ trả về null nếu không tìm thấy hóa đơn
    }

    /// <summary>
    /// Lấy danh sách tất cả các hóa đơn của một Seller cụ thể,
    /// sắp xếp theo ngày tạo mới nhất.
    /// </summary>
    public async Task<IEnumerable<Invoice>> GetInvoicesForSellerAsync(string sellerId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.SellerUserId == sellerId)
            .OrderByDescending(i => i.CreatedAt) // Sắp xếp để hóa đơn mới nhất lên đầu
            .AsNoTracking()
            .ToListAsync();

        return invoices;
    }

    public async Task<int> LogWebhookAsync(string gateway, string payload, string signature)
    {
        var log = new WebhookLog
        {
            PaymentGateway = gateway,
            RawPayload = payload,
            Signature = signature,
            ProcessingStatus = "Received"
        };
        _context.WebhookLogs.Add(log);
        await _context.SaveChangesAsync();
        return log.WebhookLogId;
    }

    public async Task ProcessPayOSWebhookAsync(int webhookLogId)
    {
        var log = await _context.WebhookLogs.FindAsync(webhookLogId);
        if (log == null || log.ProcessingStatus != "Received") return;

        try
        {
            var payload = JsonConvert.DeserializeObject<WebhookType>(log.RawPayload);
          //  var payload = JsonConvert.DeserializeObject<PayOSWebhookPayload>(log.RawPayload);
            // THAY ĐỔI 2: Truyền toàn bộ đối tượng payload vào hàm verify
            WebhookData verifiedData = _payOS.verifyPaymentWebhookData(payload);

            log.ProcessingStatus = "Verified";

            if (verifiedData?.desc?.Equals("success", StringComparison.OrdinalIgnoreCase) == true)
            {
                int invoiceId = (int)verifiedData.orderCode;
                var invoice = await _context.Invoices.FindAsync(invoiceId);

                if (invoice != null && invoice.Status != "Paid")
                {
                    invoice.Status = "Paid";
                    invoice.AmountPaid = verifiedData.amount;

                    _context.Payments.Add(new Payment
                    {
                        InvoiceId = invoiceId,
                        Amount = verifiedData.amount,
                        PaymentMethod = "PayOS",
                        TransactionId = verifiedData.reference // Lấy mã tham chiếu làm TransactionId
                    });

                    _context.InvoiceHistories.Add(new InvoiceHistory { InvoiceId = invoiceId, Action = "Payment received via PayOS Webhook" });
                    log.ProcessingStatus = "Processed";
                    log.RelatedInvoiceId = invoiceId;
                }
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            log.ProcessingStatus = "Failed";
            log.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync();
            // Bạn có thể log lỗi chi tiết ở đây nếu cần
            throw;
        }
    }
}