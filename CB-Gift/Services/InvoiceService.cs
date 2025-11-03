using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Net.payOS;
using Net.payOS.Types;
using Newtonsoft.Json;

namespace CB_Gift.Services;

public class InvoiceService : IInvoiceService
{
    private readonly CBGiftDbContext _context;
    private readonly PayOS _payOS;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(CBGiftDbContext context, IConfiguration configuration,INotificationService notificationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _hubContext = hubContext; 
        _logger = logger;
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
            List<Order> uninvoicedOrders;

            // KỊCH BẢN 1: TẠO HÓA ĐƠN THEO DANH SÁCH ORDER ID
            if (request.OrderIds != null && request.OrderIds.Any())
            {
                // Bước 1: Lấy tất cả các đơn hàng được yêu cầu từ database
                var requestedOrders = await _context.Orders
                    .Where(o => request.OrderIds.Contains(o.OrderId))
                    .ToListAsync();

                // Bước 2: KIỂM TRA (VALIDATION)
                // Kiểm tra xem có ID nào không tồn tại không
                if (requestedOrders.Count != request.OrderIds.Distinct().Count())
                {
                    throw new KeyNotFoundException("Một hoặc nhiều OrderID không tồn tại trong hệ thống.");
                }

                // Kiểm tra xem có đơn hàng nào không thuộc về Seller được chỉ định không
                if (requestedOrders.Any(o => o.SellerUserId != request.SellerId))
                {
                    throw new InvalidOperationException("Lỗi: Một hoặc nhiều đơn hàng không thuộc về Seller đã chọn.");
                }
                // Kiểm tra xem có đơn hàng nào chưa được thanh toán không
               /* var unpaidOrders = requestedOrders.Where(o => o.PaymentStatus != "Paid").ToList();
                if (unpaidOrders.Any())
                {
                    var unpaidOrderCodes = string.Join(", ", unpaidOrders.Select(o => o.OrderCode));
                    throw new InvalidOperationException($"Không thể tạo hóa đơn. Các đơn hàng sau chưa được thanh toán: {unpaidOrderCodes}");
                }*/

                // Kiểm tra xem có đơn hàng nào đã được xuất hóa đơn trước đó chưa
                /*
                var alreadyInvoicedIds = await _context.InvoiceItems
                    .Where(ii => request.OrderIds.Contains(ii.OrderId))
                    .Select(ii => ii.OrderId)
                    .ToListAsync();

                if (alreadyInvoicedIds.Any())
                {
                    throw new InvalidOperationException($"Các đơn hàng với ID: [{string.Join(", ", alreadyInvoicedIds)}] đã được xuất hóa đơn.");
                }*/

                uninvoicedOrders = requestedOrders;
            }
            // KỊCH BẢN 2: TẠO HÓA ĐƠN THEO NGÀY THÁNG (LOGIC CŨ)
            else if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                uninvoicedOrders = await _context.Orders
                    .Where(o => o.SellerUserId == request.SellerId &&
                                o.OrderDate >= request.StartDate.Value &&
                                o.OrderDate <= request.EndDate.Value &&
                                o.PaymentStatus == "Unpaid" &&
                                !_context.InvoiceItems.Any(ii => ii.OrderId == o.OrderId))
                    .ToListAsync();
            }
            else
            {
                // Nếu không cung cấp đủ thông tin
                throw new ArgumentException("Phải cung cấp danh sách Order ID hoặc khoảng thời gian (StartDate và EndDate).");
            }

            // --- PHẦN LOGIC CHUNG (TỪ ĐÂY TRỞ XUỐNG KHÔNG THAY ĐỔI) ---

            if (!uninvoicedOrders.Any())
                throw new InvalidOperationException("Không có đơn hàng mới hợp lệ để tạo hóa đơn.");

            var subtotal = uninvoicedOrders.Sum(o => o.TotalCost ?? 0);

            var newInvoice = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                SellerUserId = request.SellerId,
                CreatedByStaffId = staffId,
                // Sử dụng ngày của đơn hàng đầu tiên và cuối cùng nếu tạo theo OrderID
                InvoicePeriodStart = request.StartDate ?? uninvoicedOrders.Min(o => o.OrderDate),
                InvoicePeriodEnd = request.EndDate ?? uninvoicedOrders.Max(o => o.OrderDate),
                DueDate = (request.EndDate ?? uninvoicedOrders.Max(o => o.OrderDate)).AddDays(15), // DueDate cong 15 ngay.
                Subtotal = subtotal,
                TotalAmount = subtotal,
                Status = "Issued",
                Notes = request.Notes
            };

            _context.Invoices.Add(newInvoice);
            await _context.SaveChangesAsync();

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
            // ✅ BẮT ĐẦU GỬI THÔNG BÁO (SAU KHI COMMIT THÀNH CÔNG)
            try
            {
                // 1. Gửi thông báo (chuông) đến Seller
                await _notificationService.CreateAndSendNotificationAsync(
                    newInvoice.SellerUserId,
                    $"Bạn có một hóa đơn mới #{newInvoice.InvoiceNumber} với tổng tiền {newInvoice.TotalAmount:C0}.",
                    $"/seller/invoices/{newInvoice.InvoiceId}" // Link để seller xem hóa đơn
                );

                // 2. Gửi sự kiện real-time để cập nhật UI
                // Gửi đến "phòng" của Seller
                await _hubContext.Clients.Group($"user_{newInvoice.SellerUserId}").SendAsync(
                    "NewInvoiceCreated",
                    newInvoice // Gửi đi đối tượng hóa đơn vừa tạo
                );
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nhưng không làm hỏng kết quả trả về
                _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho CreateInvoiceAsync (InvoiceID: {InvoiceId})", newInvoice.InvoiceId);
            }
            // ✅ KẾT THÚC GỬI THÔNG BÁO
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
            .Include(i => i.SellerUser)
            .Include(i => i.CreatedByStaff)
            .Include(i => i.Payments)
            .Include(i => i.History)
            .Include(i => i.Items)
                // Từ Items, tải Order
                .ThenInclude(item => item.Order)
                    // Từ Order, tải StatusOrderNavigation
                    .ThenInclude(order => order.StatusOrderNavigation)
            .Include(i => i.Items)
                // Từ Items, tải Order
                .ThenInclude(item => item.Order)
                    // Từ Order, tải danh sách OrderDetails của nó
                    .ThenInclude(order => order.OrderDetails)
                    .ThenInclude(detail => detail.ProductVariant)
                        .ThenInclude(variant => variant.Product)

            .AsNoTracking() // Bạn có thể thử giữ lại dòng này với cấu trúc Include mới
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        return invoice;
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
    public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
    {
        // Truy vấn đơn giản, không có logic Skip/Take
        var invoices = await _context.Invoices
            .Include(i => i.SellerUser) // Tải kèm thông tin Seller để hiển thị tên
            .OrderByDescending(i => i.CreatedAt) // Sắp xếp hóa đơn mới nhất lên đầu
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
        Invoice invoiceForNotification = null;
        List<Order> ordersForNotification = new List<Order>();
        try
        {
            var payload = JsonConvert.DeserializeObject<WebhookType>(log.RawPayload);

            WebhookData verifiedData = _payOS.verifyPaymentWebhookData(payload);
            log.ProcessingStatus = "Verified";

            // Kiểm tra "success" 
            if (verifiedData?.desc?.Equals("success", StringComparison.OrdinalIgnoreCase) == true || verifiedData?.desc?.Equals("Thành công", StringComparison.OrdinalIgnoreCase) == true)
            {
                int invoiceId = (int)verifiedData.orderCode;
                var invoice = await _context.Invoices
                              .Include(i=>i.Items)
                              .FirstOrDefaultAsync(i=>i.InvoiceId==invoiceId);

                // Xử lý trường hợp không tìm thấy hóa đơn
                if (invoice == null)
                {
                    // Biến lỗi "im lặng" thành lỗi rõ ràng
                    throw new Exception($"Webhook đã được xác thực nhưng không tìm thấy Invoice với ID: {invoiceId} trong database.");
                }
                // Gán giá trị để gửi thông báo sau
                invoiceForNotification = invoice;
                if (invoice.Status != "Paid")
                {
                    // Logic cập nhật đúng
                    invoice.Status = "Paid";
                    invoice.AmountPaid = verifiedData.amount;

                    _context.Payments.Add(new Payment
                    {
                        InvoiceId = invoiceId,
                        Amount = verifiedData.amount,
                        PaymentMethod = "PayOS",
                        TransactionId = verifiedData.reference
                    });
                    //  Cập nhật trạng thái cho tất cả các Order liên quan
                    var orderIdsToUpdate = invoice.Items.Select(item => item.OrderId).ToList(); // select các OrderID có trong hóa đơn
                    var ordersToUpdate = await _context.Orders
                        .Where(o => orderIdsToUpdate.Contains(o.OrderId)) // dựa vào OrderID để lấy ra list Object [Order] để cập nhật
                        .ToListAsync();

                    foreach (var order in ordersToUpdate)
                    {
                        order.PaymentStatus = "Paid"; // cập nhật trạng thái Order thành Paid
                    }
                    // Gán giá trị để gửi thông báo sau
                    ordersForNotification = ordersToUpdate;
                    _context.InvoiceHistories.Add(new InvoiceHistory { InvoiceId = invoiceId, Action = "Payment received via PayOS Webhook" });

                    log.ProcessingStatus = "Processed";
                    log.RelatedInvoiceId = invoiceId;
                }
                else
                {
                    // Xử lý trường hợp webhook đến muộn hoặc bị gọi lại
                    log.ProcessingStatus = "AlreadyProcessed";
                    log.RelatedInvoiceId = invoiceId;
                }
            }
            else
            {
                // Ghi nhận trường hợp giao dịch không thành công
                log.ProcessingStatus = "TransactionNotSuccessful";
                log.ErrorMessage = $"Transaction description: {verifiedData?.desc}";
                log.RelatedInvoiceId = (int?)verifiedData?.orderCode;
            }

            await _context.SaveChangesAsync();
            // ✅ BẮT ĐẦU GỬI THÔNG BÁO (SAU KHI LƯU DB THÀNH CÔNG)
            // Chỉ gửi nếu có hóa đơn được cập nhật
            if (invoiceForNotification != null && log.ProcessingStatus == "Processed")
            {
                await SendPaymentSuccessNotificationsAsync(invoiceForNotification, ordersForNotification);
            }
        }
        catch (Exception ex)
        {
            log.ProcessingStatus = "Failed";
            log.ErrorMessage = $"Message: {ex.Message} --- StackTrace: {ex.StackTrace}";
            await _context.SaveChangesAsync();
            throw; // Ném lại lỗi để dễ dàng thấy trong log của server
        }
    }
    /// <summary>
    /// Hàm private để gửi thông báo sau khi webhook xử lý thanh toán thành công.
    /// </summary>
    private async Task SendPaymentSuccessNotificationsAsync(Invoice invoice, List<Order> orders)
    {
        try
        {
            // Gửi thông báo (chuông) cho Seller
            await _notificationService.CreateAndSendNotificationAsync(
                invoice.SellerUserId,
                $"Hóa đơn #{invoice.InvoiceNumber} đã được thanh toán thành công!",
                $"/seller/invoices/{invoice.InvoiceId}"
            );

            // Gửi sự kiện real-time cập nhật Giao diện Hóa đơn (cho Seller)
            await _hubContext.Clients.Group($"user_{invoice.SellerUserId}").SendAsync(
                "InvoicePaid",
                new { invoiceId = invoice.InvoiceId, newStatus = "Paid" }
            );

            // Gửi sự kiện real-time cập nhật Giao diện Đơn hàng (cho từng đơn hàng)
            foreach (var order in orders)
            {
                // Gửi cho bất kỳ ai đang xem đơn hàng này
                await _hubContext.Clients.Group($"order_{order.OrderId}").SendAsync(
                    "OrderStatusChanged",
                    new { orderId = order.OrderId, newPaymentStatus = "Paid" }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho ProcessPayOSWebhookAsync (InvoiceID: {InvoiceId})", invoice.InvoiceId);
        }
    }
}