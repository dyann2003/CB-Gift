using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
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
            //huy
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
                    throw new InvalidOperationException("Một hoặc nhiều đơn hàng không thuộc về Seller đã chọn.");
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

            // --- PHẦN LOGIC CHUNG 

            if (!uninvoicedOrders.Any())
                throw new InvalidOperationException("Không có đơn hàng mới hợp lệ để tạo hóa đơn.");

            var subtotal = uninvoicedOrders.Sum(o => o.TotalCost ?? 0);
            decimal discountAmount = 0;
            int? appliedDiscountId = null;
            if (!string.IsNullOrEmpty(request.DiscountCode))
            {
                // Tìm mã chiết khấu trong DB
                var discount = await _context.Discounts
                    .FirstOrDefaultAsync(d => d.Code.ToUpper() == request.DiscountCode.ToUpper() &&
                                            d.IsActive &&
                                            d.StartDate <= DateTime.UtcNow &&
                                            (d.EndDate == null || d.EndDate >= DateTime.UtcNow));

                if (discount == null)
                {
                    throw new InvalidOperationException("Mã chiết khấu không hợp lệ hoặc đã hết hạn.");
                }

                if (subtotal < discount.MinApplicableAmount)
                {
                    throw new InvalidOperationException($"Tổng tiền chưa đạt mức tối thiểu {discount.MinApplicableAmount:C0} để áp dụng mã này.");
                }

                // Tính toán số tiền chiết khấu
                if (discount.DiscountType == "Percentage")
                {
                    discountAmount = subtotal * (discount.Value / 100);
                }
                else if (discount.DiscountType == "FixedAmount")
                {
                    discountAmount = discount.Value;
                }

                appliedDiscountId = discount.DiscountId;
            }

            // 4. [CẬP NHẬT CÔNG THỨC] - Tính TotalAmount (Tổng cộng)
            var totalAmount = subtotal - discountAmount;
            if (totalAmount < 0) totalAmount = 0;
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
                DiscountAmount = discountAmount,  
                TotalAmount = totalAmount,         
                DiscountId = appliedDiscountId,
                AmountPaid = 0,
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

        // 1. - Tính toán số tiền còn lại
        decimal remainingBalance = invoice.TotalAmount - invoice.AmountPaid;

        // 2. - Xác định số tiền cần thanh toán cho LẦN NÀY
        // Nếu người dùng không nhập (null), thì thanh toán hết số nợ còn lại.
        // Nếu người dùng có nhập, thì thanh toán đúng số đó.
        decimal amountToPay = request.Amount ?? remainingBalance;

        // 3. - Validation (Kiểm tra hợp lệ)
        if (amountToPay <= 0)
            throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0.");

        if (amountToPay > remainingBalance)
            throw new InvalidOperationException($"Số tiền thanh toán ({amountToPay:C0}) không thể lớn hơn số nợ còn lại ({remainingBalance:C0}).");

        // 4. - TẠO PAYMENT INTENT (Bản ghi Payment ở trạng thái "Pending")
        var newPayment = new Payment
        {
            InvoiceId = invoice.InvoiceId,
            PaymentDate = DateTime.UtcNow,
            Amount = amountToPay,
            PaymentMethod = "PayOS",
            Status = "Pending",      
            ProcessedByStaffId = null
        };

        _context.Payments.Add(newPayment);
        await _context.SaveChangesAsync(); // Lưu để lấy được PaymentId

        // 5. - Tạo link PayOS với thông tin từ Payment (KHÔNG PHẢI INVOICE)
        var items = new List<ItemData>
        {
            new ItemData(
                name: $"Thanh toán cho hóa đơn {invoice.InvoiceNumber}",
                quantity: 1,
                price: (int)newPayment.Amount // <-- SỬ DỤNG SỐ TIỀN CỦA LẦN NÀY
            )
        };

        var paymentData = new PaymentData(
            orderCode: newPayment.PaymentId, // <-- [RẤT QUAN TRỌNG] SỬ DỤNG PAYMENT ID LÀM ORDER CODE
            amount: (int)newPayment.Amount,
            description: $"Pay Inv: {invoice.InvoiceNumber}".Substring(0, Math.Min(25, $"Pay Inv: {invoice.InvoiceNumber}".Length)),
            items: items,
            cancelUrl: request.CancelUrl,
            returnUrl: request.ReturnUrl
        );

        CreatePaymentResult result = await _payOS.createPaymentLink(paymentData);

        // Không cần lưu PaymentLink vào Invoice nữa, vì link này là duy nhất cho 1 giao dịch

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
            .Include(i => i.Payments) // Tải danh sách các lần thanh toán
            .Include(i => i.History)
            .Include(i => i.AppliedDiscount) // [THAY ĐỔI] Tải thông tin mã chiết khấu đã áp dụng
            .Include(i => i.Items)
                .ThenInclude(item => item.Order)
                    .ThenInclude(order => order.StatusOrderNavigation)
            .Include(i => i.Items)
                .ThenInclude(item => item.Order)
                    .ThenInclude(order => order.OrderDetails)
                    .ThenInclude(detail => detail.ProductVariant)
                        .ThenInclude(variant => variant.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        return invoice;
    }

    /// <summary>
    /// Lấy danh sách tất cả các hóa đơn của một Seller cụ thể,
    /// sắp xếp theo ngày tạo mới nhất.
    /// </summary>
    public async Task<IEnumerable<Invoice>> GetInvoicesForSellerAsync(string sellerId)
    {
        return await _context.Invoices
            .Where(i => i.SellerUserId == sellerId)
            .Include(i => i.AppliedDiscount) // [THAY ĐỔI] Thêm chiết khấu vào list
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }
    public async Task<PaginatedResult<InvoiceSummaryDto>> GetInvoicesForSellerPageAsync(
    string sellerId, string? status, string? searchTerm, int page, int pageSize)
    {
        // 1. Bắt đầu với IQueryable<Invoice> cơ bản
        var query = _context.Invoices.AsQueryable();

        // 2. Áp dụng TẤT CẢ các bộ lọc (filter) TRƯỚC

        // Luôn lọc theo sellerId
        query = query.Where(i => i.SellerUserId == sellerId);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(i => i.Status == status);
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(i => i.InvoiceNumber.Contains(searchTerm));
        }

        // 3. Lấy tổng số (total) SAU KHI đã lọc
        var total = await query.CountAsync();

        // 4. BÂY GIỜ mới áp dụng Include, Sắp xếp, Phân trang, và Chọn cột
        var invoices = await query
            .Include(i => i.AppliedDiscount) // <-- Include ở đây
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceSummaryDto // Ánh xạ thủ công
            {
                InvoiceId = i.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                SellerUserId = i.SellerUserId,
                SellerName = null, // Không cần tên của chính mình
                CreatedAt = i.CreatedAt,
                DueDate = i.DueDate,
                TotalAmount = i.TotalAmount,
                AmountPaid = i.AmountPaid,
                Status = i.Status
            })
            .AsNoTracking()
            .ToListAsync();

        return new PaginatedResult<InvoiceSummaryDto> { Items = invoices, Total = total, Page = page, PageSize = pageSize };
    }
    /* public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
     {
         return await _context.Invoices
             .Include(i => i.SellerUser)
             .Include(i => i.AppliedDiscount) // [THAY ĐỔI] Thêm chiết khấu vào list
             .OrderByDescending(i => i.CreatedAt)
             .AsNoTracking()
             .ToListAsync();
     }*/
    public async Task<PaginatedResult<InvoiceSummaryDto>> GetAllInvoicesAsync(
    string? status, string? searchTerm, string? sellerId, int page, int pageSize)
    {
        // 1. Bắt đầu với IQueryable<Invoice> cơ bản
        var query = _context.Invoices.AsQueryable();

        // 2. Áp dụng TẤT CẢ các bộ lọc (filter) TRƯỚC
        if (!string.IsNullOrEmpty(status))
        {
            // Giờ đây query = query.Where() là hợp lệ (IQueryable = IQueryable)
            query = query.Where(i => i.Status == status);
        }

        if (!string.IsNullOrEmpty(sellerId))
        {
            // Hợp lệ
            query = query.Where(i => i.SellerUserId == sellerId);
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            // Hợp lệ
            // GHI CHÚ: Vì SellerUser chưa được Include,
            // EF Core sẽ tự động tạo JOIN khi cần thiết cho .Where() này
            query = query.Where(i =>
                i.InvoiceNumber.Contains(searchTerm) ||
                (i.SellerUser != null && i.SellerUser.FullName.Contains(searchTerm))
            );
        }

        // 3. Lấy tổng số (total) SAU KHI đã lọc
        var total = await query.CountAsync();

        // 4. BÂY GIỜ mới áp dụng Include, Sắp xếp, Phân trang, và Chọn cột
        var invoices = await query
            .Include(i => i.SellerUser)       // <-- Include ở đây
            .Include(i => i.AppliedDiscount)  // <-- Include ở đây
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceSummaryDto // Ánh xạ thủ công
            {
                InvoiceId = i.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                SellerUserId = i.SellerUserId,
                SellerName = i.SellerUser.FullName,
                CreatedAt = i.CreatedAt,
                DueDate = i.DueDate,
                TotalAmount = i.TotalAmount,
                AmountPaid = i.AmountPaid,
                Status = i.Status
            })
            .AsNoTracking()
            .ToListAsync();

        return new PaginatedResult<InvoiceSummaryDto> { Items = invoices, Total = total, Page = page, PageSize = pageSize };
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

    // [CẬP NHẬT] - Toàn bộ logic webhook để xử lý công nợ
    public async Task ProcessPayOSWebhookAsync(int webhookLogId)
    {
        var log = await _context.WebhookLogs.FindAsync(webhookLogId);
        if (log == null || log.ProcessingStatus != "Received") return;

        Invoice invoiceForNotification = null;
        List<Order> ordersForNotification = new List<Order>();
        Payment paymentForNotification = null;

        try
        {
            var payload = JsonConvert.DeserializeObject<WebhookType>(log.RawPayload);
            WebhookData verifiedData = _payOS.verifyPaymentWebhookData(payload);
            log.ProcessingStatus = "Verified";

            if (verifiedData?.desc?.Equals("success", StringComparison.OrdinalIgnoreCase) == true || verifiedData?.desc?.Equals("Thành công", StringComparison.OrdinalIgnoreCase) == true)
            {
                // 1. [THAY ĐỔI] - LẤY PAYMENT ID TỪ WEBHOOK (thay vì InvoiceId)
                int paymentId = (int)verifiedData.orderCode;
                var payment = await _context.Payments.FindAsync(paymentId);

                if (payment == null)
                {
                    throw new Exception($"Webhook đã xác thực nhưng không tìm thấy Payment với ID: {paymentId}.");
                }

                paymentForNotification = payment;

                // 2. [THAY ĐỔI] - Kiểm tra xem đã xử lý chưa (dựa trên trạng thái của Payment)
                if (payment.Status != "Pending")
                {
                    log.ProcessingStatus = "AlreadyProcessed";
                    log.RelatedInvoiceId = payment.InvoiceId;
                    await _context.SaveChangesAsync();
                    return; // Đã xử lý rồi, dừng lại
                }

                // 3. Lấy Invoice cha
                var invoice = await _context.Invoices
                                    .Include(i => i.Items)
                                    .FirstOrDefaultAsync(i => i.InvoiceId == payment.InvoiceId);

                if (invoice == null)
                {
                    throw new Exception($"Không tìm thấy Invoice (ID: {payment.InvoiceId}) cho Payment (ID: {paymentId}).");
                }

                invoiceForNotification = invoice;

                // 4. Cập nhật bản ghi Payment
                payment.Status = "Completed";
                payment.TransactionId = verifiedData.reference;

                if (payment.Amount != verifiedData.amount)
                {
                    _logger.LogWarning("Payment (ID: {PaymentId}) amount mismatch. Expected: {ExpectedAmount}, Received: {ReceivedAmount}",
                        payment.PaymentId, payment.Amount, verifiedData.amount);
                    payment.Amount = verifiedData.amount; // Tin tưởng số tiền PayOS trả về
                }

                // 5. [THAY ĐỔI] - CẬP NHẬT INVOICE (CỘNG DỒN)
                invoice.AmountPaid += payment.Amount;

                // 6. [THAY ĐỔI] - KIỂM TRA VÀ CẬP NHẬT TRẠNG THÁI INVOICE
                if (invoice.AmountPaid >= invoice.TotalAmount)
                {
                    // Thanh toán đủ
                    invoice.Status = "Paid";
                    invoice.AmountPaid = invoice.TotalAmount; // Chốt số tiền (tránh lỗi làm tròn)

                    log.ProcessingStatus = "Processed_Full";

                    // Cập nhật trạng thái cho tất cả các Order liên quan
                    var orderIdsToUpdate = invoice.Items.Select(item => item.OrderId).ToList();
                    var ordersToUpdate = await _context.Orders
                        .Where(o => orderIdsToUpdate.Contains(o.OrderId))
                        .ToListAsync();

                    foreach (var order in ordersToUpdate)
                    {
                        order.PaymentStatus = "Paid";
                    }
                    ordersForNotification = ordersToUpdate;
                }
                else
                {
                    // [THAY ĐỔI] - Thanh toán một phần (CÔNG NỢ)
                    invoice.Status = "PartiallyPaid";
                    log.ProcessingStatus = "Processed_Partial";
                }

                _context.InvoiceHistories.Add(new InvoiceHistory
                {
                    InvoiceId = invoice.InvoiceId,
                    Action = $"Payment {payment.Amount:C0} received via PayOS. (PaymentID: {payment.PaymentId})",
                    UserId = null
                });

                log.RelatedInvoiceId = invoice.InvoiceId;
            }
            else
            {
                // [THAY ĐỔI] - Xử lý khi giao dịch thất bại (cập nhật Payment)
                int paymentId = (int)verifiedData.orderCode;
                var payment = await _context.Payments.FindAsync(paymentId);
                if (payment != null)
                {
                    payment.Status = "Failed";
                    log.RelatedInvoiceId = payment.InvoiceId;
                }
                log.ProcessingStatus = "TransactionNotSuccessful";
                log.ErrorMessage = $"Transaction description: {verifiedData?.desc}";
            }

            await _context.SaveChangesAsync();

            // Gửi thông báo (chỉ khi xử lý thành công)
            if (invoiceForNotification != null && paymentForNotification != null &&
                (log.ProcessingStatus == "Processed_Full" || log.ProcessingStatus == "Processed_Partial"))
            {
                // [THAY ĐỔI] - Gọi hàm helper mới
                await SendPaymentUpdateNotificationsAsync(invoiceForNotification, ordersForNotification, paymentForNotification);
            }
        }
        catch (Exception ex)
        {
            log.ProcessingStatus = "Failed";
            log.ErrorMessage = $"Message: {ex.Message} --- StackTrace: {ex.StackTrace}";
            await _context.SaveChangesAsync();
            throw;
        }
    }

    // [CẬP NHẬT] - Hàm helper mới để gửi thông báo cho cả 2 trường hợp (Partial/Full)
    private async Task SendPaymentUpdateNotificationsAsync(Invoice invoice, List<Order> orders, Payment payment)
    {
        try
        {
            // Gửi thông báo (chuông) cho Seller
            string message;
            if (invoice.Status == "Paid")
            {
                message = $"Hóa đơn #{invoice.InvoiceNumber} đã được thanh toán đầy đủ!";
            }
            else // PartiallyPaid
            {
                message = $"Hóa đơn #{invoice.InvoiceNumber} vừa nhận được một khoản thanh toán công nợ {payment.Amount:C0}.";
            }

            await _notificationService.CreateAndSendNotificationAsync(
                invoice.SellerUserId,
                message,
                $"/seller/invoices/{invoice.InvoiceId}"
            );

            // Gửi sự kiện real-time cập nhật Giao diện Hóa đơn (cho Seller)
            await _hubContext.Clients.Group($"user_{invoice.SellerUserId}").SendAsync(
                "InvoiceUpdated",
                new
                {
                    invoiceId = invoice.InvoiceId,
                    newStatus = invoice.Status,
                    amountPaid = invoice.AmountPaid,
                    remainingBalance = invoice.TotalAmount - invoice.AmountPaid
                }
            );

            // Chỉ cập nhật trạng thái order qua SignalR nếu hóa đơn đã "Paid"
            if (invoice.Status == "Paid")
            {
                foreach (var order in orders)
                {
                    await _hubContext.Clients.Group($"order_{order.OrderId}").SendAsync(
                        "OrderStatusChanged",
                        new { orderId = order.OrderId, newPaymentStatus = "Paid" }
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo SignalR cho SendPaymentUpdateNotificationsAsync (InvoiceID: {InvoiceId})", invoice.InvoiceId);
        }
    }
    public async Task<OverdueCheckDto> CheckForOverdueInvoiceAsync(string sellerId)
    {
        var now = DateTime.UtcNow; // Dùng UtcNow để nhất quán

        // Tìm hóa đơn CŨ NHẤT (OrderBy DueDate)
        // đáp ứng cả 3 điều kiện:
        var overdueInvoice = await _context.Invoices
            .Where(i =>
                // 1. Là của seller này
                i.SellerUserId == sellerId &&
                // 2. Chưa thanh toán xong
                (i.Status == "Issued" || i.Status == "PartiallyPaid") &&
                // 3. Đã quá hạn
                i.DueDate < now
            )
            .OrderBy(i => i.DueDate) // Lấy hóa đơn cũ nhất
            .Select(i => new { i.InvoiceId }) // Chỉ cần lấy ID
            .FirstOrDefaultAsync();

        if (overdueInvoice == null)
        {
            return new OverdueCheckDto { HasOverdueInvoice = false };
        }

        return new OverdueCheckDto
        {
            HasOverdueInvoice = true,
            OverdueInvoiceId = overdueInvoice.InvoiceId
        };
    }
     public async Task<PaginatedResult<SellerReceivablesDto>> GetSellerReceivablesAsync(
    string? searchTerm, string? sortColumn, string? sortDirection, int page, int pageSize)
    {
        // 1. Lấy thông tin tổng hợp từ Invoices, nhóm theo Seller
        // GHI CHÚ: 'g' là một nhóm các hóa đơn của 1 seller
        var sellerStats = _context.Invoices
            .GroupBy(i => i.SellerUserId)
            .Select(g => new
            {
                SellerId = g.Key,
                // Tổng doanh số (tổng giá trị tất cả hóa đơn)
                TotalSales = g.Sum(i => i.TotalAmount),
                // Tổng công nợ (chỉ tính các hóa đơn chưa trả xong)
                TotalDebt = g.Sum(i => (i.Status == "Issued" || i.Status == "PartiallyPaid")
                                        ? (i.TotalAmount - i.AmountPaid)
                                        : 0)
            });

        // 2. Join với bảng Users để lấy thông tin chi tiết của Seller
        // Giả định: Bảng User của bạn là 'Users' và có 'FullName', 'PhoneNumber'
        var query = _context.Users
            .Join(sellerStats,
                  user => user.Id,       // Key từ bảng Users
                  stats => stats.SellerId, // Key từ bảng sellerStats
                  (user, stats) => new SellerReceivablesDto
                  {
                      Id = user.Id,
                      Name = user.FullName, 
                      Email = user.Email,
                      Phone = user.PhoneNumber, 
                      TotalSales = stats.TotalSales,
                      TotalDebt = stats.TotalDebt
                  });

        // 3. Áp dụng tìm kiếm (Search)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(s =>
                s.Name.Contains(searchTerm) ||
                s.Email.Contains(searchTerm) ||
                s.Phone.Contains(searchTerm)
            );
        }

        // 4. Áp dụng Sắp xếp (Sorting) - (Bạn có thể mở rộng sau)
        // Mặc định sắp xếp theo công nợ giảm dần
        query = query.OrderByDescending(s => s.TotalDebt);

        // 5. Phân trang
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<SellerReceivablesDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
    // [CẬP NHẬT] - Logic cho tab "Payment History"
    public async Task<PaginatedResult<PaymentSummaryDto>> GetPaymentsForSellerAsync(string sellerId, int page, int pageSize)
    {
        var query = _context.Payments
            .Include(p => p.Invoice)
            .Where(p => p.Invoice.SellerUserId == sellerId)
            .OrderByDescending(p => p.PaymentDate);

        var total = await query.CountAsync();

        var payments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentSummaryDto
            {
                PaymentId = p.PaymentId,
                InvoiceId = p.InvoiceId,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                Status = p.Status, // Trạng thái của Payment
                TransactionId = p.TransactionId,
                InvoiceTotal = p.Invoice.TotalAmount,
                InvoiceStatus = p.Invoice.Status, // Trạng thái của Invoice (Paid, Partial)
                InvoiceRemaining = p.Invoice.TotalAmount - p.Invoice.AmountPaid
            })
            .AsNoTracking()
            .ToListAsync();

        return new PaginatedResult<PaymentSummaryDto> { Items = payments, Total = total, Page = page, PageSize = pageSize };
    }

    // [THÊM MỚI] - Logic cho tab "Sales History" (Lấy danh sách các tháng)
    public async Task<List<SellerMonthlySalesDto>> GetSellerMonthlySalesAsync(string sellerId)
    {
        // 1. Lấy tất cả các order "Đã Ship" (SHIPPED) của seller
        var ordersInMonth = await _context.Orders
            .Include(o => o.StatusOrderNavigation) // Cần để lọc theo Code
            .Where(o => o.SellerUserId == sellerId && o.StatusOrderNavigation.Code == "SHIPPED")
            .Select(o => new
            {
                o.OrderId,
                o.OrderDate,
                o.TotalCost,
                // Kiểm tra xem order này đã nằm trong invoice nào chưa
                IsInvoiced = _context.InvoiceItems.Any(ii => ii.OrderId == o.OrderId)
            })
            .ToListAsync();

        // 2. Nhóm bằng code C#
        var monthlyGroups = ordersInMonth
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new SellerMonthlySalesDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                TotalAmount = g.Sum(o => o.TotalCost ?? 0),
                // Logic xác định trạng thái của tháng
                Status = g.All(o => o.IsInvoiced) ? "Invoiced" // Tất cả đã được lập hóa đơn
                       : g.Any(o => o.IsInvoiced) ? "PartiallyInvoiced" // Một phần đã lập
                       : "Uninvoiced" // Chưa lập
            })
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .ToList();

        return monthlyGroups;
    }

    // [THÊM MỚI] - Logic cho tab "Sales History" (Lấy order của 1 tháng)
    public async Task<PaginatedResult<SellerOrderDto>> GetSellerOrdersForMonthAsync(string sellerId, int year, int month, int page, int pageSize)
    {
        var query = _context.Orders
            .Include(o => o.EndCustomer)
            .Include(o => o.StatusOrderNavigation)
            .Where(o =>
                o.SellerUserId == sellerId &&
                o.OrderDate.Year == year &&
                o.OrderDate.Month == month &&
                o.StatusOrderNavigation.Code == "SHIPPED"
            )
            .OrderByDescending(o => o.OrderDate);

        var total = await query.CountAsync();

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new SellerOrderDto
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                OrderDate = o.OrderDate,
                CustomerName = o.EndCustomer.Name, // Giả định tên khách hàng
                TotalCost = o.TotalCost ?? 0,
                OrderStatus = o.StatusOrderNavigation.Code // Giả định tên trạng thái
            })
            .ToListAsync();

        return new PaginatedResult<SellerOrderDto> { Items = orders, Total = total, Page = page, PageSize = pageSize };
    }

    // [THÊM MỚI] - Logic cho nút "Create Monthly Receipt"
    public async Task<Invoice> CreateInvoiceForMonthAsync(CreateMonthlyInvoiceRequest request, string staffId)
    {
        // 1. Tìm tất cả các Order ID "Đã Ship" CHƯA được lập hóa đơn
        var orderIdsToInvoice = await _context.Orders
            .Include(o => o.StatusOrderNavigation) // Cần để lọc "SHIPPED"
            .Where(o =>
                o.SellerUserId == request.SellerId &&
                o.OrderDate.Year == request.Year &&
                o.OrderDate.Month == request.Month &&
                o.StatusOrderNavigation.Code == "SHIPPED" &&
                !_context.InvoiceItems.Any(ii => ii.OrderId == o.OrderId) // CHƯA CÓ HÓA ĐƠN
            )
            .Select(o => o.OrderId)
            .ToListAsync();

        if (orderIdsToInvoice == null || !orderIdsToInvoice.Any())
        {
            throw new InvalidOperationException("Không có đơn hàng 'Đã Ship' nào mới (chưa lập hóa đơn) trong tháng này.");
        }

        // 2. Tạo request DTO cho hàm CreateInvoiceAsync CŨ
        var createInvoiceRequest = new CreateInvoiceRequest
        {

            SellerId = request.SellerId,
            OrderIds = orderIdsToInvoice,
            Notes = request.Notes ?? $"Hóa đơn tổng hợp tháng {request.Month}/{request.Year}",
            StartDate = new DateTime(request.Year, request.Month, 1),
            EndDate = new DateTime(request.Year, request.Month, 1).AddMonths(1).AddDays(-1)
        };

        // 3. Gọi hàm tạo hóa đơn gốc (mà bạn đã cung cấp)
        return await CreateInvoiceAsync(createInvoiceRequest, staffId);
    }


}