using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Hubs;
using CB_Gift.Models.Enums;
using CB_Gift.Services;
using CB_Gift.Services.IService;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CB_Gift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Seller")]
    public class SellerController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IDesignerTaskService _designerTaskService;
        private readonly IDesignerSellerService _designerSellerservice;
        private readonly IHubContext<NotificationHub> _hubContext; // <-- SignalR hub context

        private readonly CBGiftDbContext _context;
        private readonly ILogger<SellerController> _logger;

        // Cập nhật constructor để inject IHubContext<NotificationHub>
        // Cập nhật constructor để inject IHubContext<NotificationHub>
        public SellerController(
        IOrderService orderService,
        IDesignerTaskService designerTaskService,
        IDesignerSellerService designerSellerservice,
        IHubContext<NotificationHub> hubContext,
        CBGiftDbContext context,                // <--- thêm
        ILogger<SellerController> logger         // <--- thêm
    )
        {
            _orderService = orderService;
            _designerTaskService = designerTaskService;
            _designerSellerservice = designerSellerservice;
            _hubContext = hubContext;
            _context = context;                      // <--- gán
            _logger = logger;                        // <--- gán
        }

        // ----------------------------------------------------------------------
        // ✅ API ĐÃ SỬA: Hỗ trợ Phân trang, Lọc, Tìm kiếm, Sắp xếp (DÙNG SERVICE MỚI)
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] string? status = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? sortColumn = null,
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                // ✅ Gọi Service mới đã được tối ưu hóa
                var (orders, total) = await _orderService.GetFilteredAndPagedOrdersForSellerAsync(
                    sellerId, status, searchTerm, sortColumn, sortDirection, fromDate, toDate, page, pageSize);

                // ✅ Trả về tổng số lượng và dữ liệu của trang hiện tại
                return Ok(new { total, orders });
            }
            catch (Exception ex)
            {
                // Có thể log lỗi ở đây
                return StatusCode(500, new { message = "Đã xảy ra lỗi khi lấy danh sách đơn hàng.", detail = ex.Message });
            }
        }

        // ----------------------------------------------------------------------
        // Giữ nguyên các API khác
        // ----------------------------------------------------------------------

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateRequest request)
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var orderId = await _orderService.CreateOrderAsync(request, sellerId);
            return Ok(new { message = "Order created successfully.", orderId });
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderDetail(int id)
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            // ✅ Đảm bảo gọi API chi tiết đơn hàng riêng biệt
            var result = await _orderService.GetOrderDetailAsync(id, sellerId);
            return result == null ? NotFound() : Ok(result);
        }


        [HttpPost("{orderId}/details")]
        public async Task<IActionResult> AddOrderDetail(int orderId, [FromBody] OrderDetailCreateRequest request)
        {
            string sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            try
            {
                // Gọi Service (đã thêm kiểm tra Variant ID)
                await _orderService.AddOrderDetailAsync(orderId, request, sellerId);
                return Ok(new { message = "Order detail added successfully." });
            }
            catch (ArgumentException ex)
            {
                // Bắt lỗi khi ProductVariantID không tồn tại
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Bắt các lỗi chung khác
                return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        // Endpoint để giao việc
        // POST: /api/seller/tasks/order-details/123/assign
        [HttpPost("order-details/{orderDetailId}/assign")]
        public async Task<IActionResult> AssignDesigner(int orderDetailId, [FromBody] AssignDesignerToOrderDetailDto dto)
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

            try
            {
                var success = await _designerTaskService.AssignDesignerToOrderDetailAsync(orderDetailId, dto.DesignerUserId, sellerId);

                if (!success)
                {
                    return NotFound("Không tìm thấy Chi tiết đơn hàng hoặc bạn không có quyền truy cập.");
                }

                return Ok(new { message = "Giao việc cho Designer thành công. Đơn hàng đã chuyển sang trạng thái Cần Design." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "Đã xảy ra lỗi không mong muốn.");
            }
        }
        //assign theo OrderId
        // POST: /api/seller/orders/{orderId}/assign-designer
        //[HttpPost("orders/{orderId}/assign-designer")]
        //public async Task<IActionResult> AssignDesignerToOrder(int orderId, [FromBody] AssignDesignerToOrderDetailDto dto)
        //{
        //    try
        //    {
        //        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

        //        // Gọi phương thức service mới
        //        var success = await _designerTaskService.AssignDesignerToOrderAsync(orderId, dto.DesignerUserId, sellerId);

        //        if (!success)
        //        {
        //            return NotFound(new { message = "Không tìm thấy Đơn hàng hoặc bạn không có quyền truy cập." });
        //        }

        //        return Ok(new { message = "Giao việc cho Designer thành công. Toàn bộ đơn hàng đã được cập nhật." });
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //    catch (Exception)
        //    {
        //        // _logger.LogError(...)
        //        return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn." });
        //    }
        //}

        [HttpPost("orders/{orderId}/assign-designer")]
        public async Task<IActionResult> AssignDesignerToOrder(int orderId, [FromBody] AssignDesignerToOrderDetailDto dto)
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

                // Gọi phương thức service mới
                var success = await _designerTaskService.AssignDesignerToOrderAsync(orderId, dto.DesignerUserId, sellerId);

                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy Đơn hàng hoặc bạn không có quyền truy cập." });
                }

                // --- Gửi notification qua SignalR đến designer (không block response nếu gửi thất bại) ---
                try
                {
                    var payload = new
                    {
                        OrderId = orderId,
                        Message = "Bạn vừa được giao một đơn hàng mới.",
                        AssignedBy = sellerId,
                        TimeUtc = DateTime.UtcNow
                    };

                    // group name format: user_{userId} (phù hợp với NotificationHub.OnConnectedAsync)
                    await _hubContext.Clients.Group($"user_{dto.DesignerUserId}")
                        .SendAsync("NewTaskAssigned", payload);
                }
                catch (Exception sendEx)
                {
                    // Log ở đây nếu bạn có logger (không trả lỗi cho client vì assign đã thành công)
                    Console.WriteLine("SignalR notify failed: " + sendEx.Message);
                }

                return Ok(new { message = "Giao việc cho Designer thành công. Toàn bộ đơn hàng đã được cập nhật." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "AssignDesignerToOrder failed");
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn.", detail = ex.Message });
            }
        }

        //Get Designer của seller
        [HttpGet("my-designer")]
        public async Task<IActionResult> GetDesignersForSeller()
        {
            try
            {
                var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(sellerId))
                {
                    return Unauthorized(new { message = "Không thể xác định người dùng. Vui lòng đăng nhập lại." });
                }

                var result = await _designerSellerservice.GetDesignersForSellerAsync(sellerId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Ghi lại log lỗi chi tiết ở phía server để bạn có thể điều tra
                // _logger.LogError(ex, "Lỗi xảy ra khi Seller {SellerId} lấy danh sách designer.", sellerId);

                // Chỉ trả về một thông báo lỗi chung chung cho client
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau." });
            }
        }
        [HttpPut("orders/{orderId}/approve-or-reject-design")]
        public async Task<IActionResult> UpdateDesignStatus(
        int orderId,
        [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // LẤY SELLER ID DƯỚI DẠNG STRING
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(sellerId))
            {
                return Unauthorized("User is not authenticated or Seller ID missing.");
            }

            var action = request.ProductionStatus;

            // Kiểm tra tính hợp lệ của Action (phải là DESIGN_REDO (5) hoặc READY_PROD (6))
            if (action != ProductionStatus.DESIGN_REDO && action != ProductionStatus.READY_PROD)
            {
                return BadRequest($"Invalid action. Must be {ProductionStatus.DESIGN_REDO} (DESIGN_REDO) or {ProductionStatus.READY_PROD} (READY_PROD).");
            }

            try
            {
                // GỌI SERVICE VỚI SELLER ID LÀ STRING
                bool success = await _orderService.SellerApproveOrderDesignAsync(
                    orderId,
                    action,
                    sellerId
                );

                if (success)
                {
                    var actionText = action == ProductionStatus.READY_PROD
                                        ? "CONFIRMED (Chốt Đơn)"
                                        : "DESIGN_REDO (Thiết Kế Lại)";

                    return Ok(new { message = $"Order {orderId} design successfully updated to {actionText}." });
                }
                else
                {
                    return NotFound($"Order with ID {orderId} not found.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error during status update.");
            }
        }
        [HttpPut("order/order-details/{orderDetailId}/design-status")]
        public async Task<IActionResult> UpdateDesignOrderDetailStatus(
    int orderDetailId,
    [FromBody] UpdateStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId))
            {
                return Unauthorized("User is not authenticated or Seller ID missing.");
            }

            var action = request.ProductionStatus;

            // Kiểm tra tính hợp lệ của Action
            if (action != ProductionStatus.DESIGN_REDO && action != ProductionStatus.READY_PROD)
            {
                return BadRequest($"Invalid action. Must be {ProductionStatus.DESIGN_REDO} or {ProductionStatus.READY_PROD}.");
            }

            // Nếu hành động là "Làm lại" (DESIGN_REDO), thì BẮT BUỘC phải có lý do
            if (action == ProductionStatus.DESIGN_REDO && string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { message = "A reason is required when rejecting a design (DESIGN_REDO)." });
            }

            try
            {
                bool success = await _orderService.SellerApproveOrderDetailDesignAsync(
                    orderDetailId,
                    action,
                    sellerId,
                    request.Reason
                );

                if (success)
                {
                    return Ok(new { message = $"OrderDetail {orderDetailId} design successfully updated to {action}." });
                }
                else
                {
                    return NotFound($"OrderDetail with ID {orderDetailId} not found.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal server error during status update.");
            }
        }
        // POST: /api/seller/orders/{orderId}/send-to-staff
        [HttpPost("orders/{orderId}/send-to-staff")]
        public async Task<IActionResult> SendOrderToReadyProd(int orderId)
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

            try
            {
                // Gọi phương thức service mới để chuyển trạng thái từ StatusOrder=1 sang 7
                var success = await _orderService.SendOrderToReadyProdAsync(orderId, sellerId);

                if (!success)
                {
                    return NotFound(new { message = $"Không tìm thấy Đơn hàng ID {orderId} hoặc bạn không có quyền truy cập." });
                }

                return Ok(new { message = "Đơn hàng đã được chốt và chuyển thẳng sang trạng thái Sẵn sàng Sản xuất (READY_PROD)." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Đã xảy ra lỗi không mong muốn khi gửi đơn hàng." });
            }
        }

        // ================================
        // 📌 API GET STATS
        // ================================
        [HttpGet("stats")]
        public async Task<IActionResult> GetMyOrderStats()
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId))
                return Unauthorized();

            var stats = await _orderService.GetOrderStatsForSellerAsync(sellerId);
            return Ok(stats);
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportAllOrdersToExcel()
        {
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sellerId))
                return Unauthorized();

            try
            {
                // 🔹 Lấy toàn bộ đơn hàng của seller (join đầy đủ)
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(d => d.ProductVariant)
                            .ThenInclude(v => v.Product)
                    .Include(o => o.EndCustomer)
                    .Include(o => o.StatusOrderNavigation)
                    .Where(o => o.SellerUserId == sellerId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("All Orders");

                // ✅ Header (đã thêm 4 cột mới)
                string[] headers =
                {
            "Order ID", "Order Code", "Order Date", "Customer Name",
            "Phone", "Email", "Address", "Status", "Total Cost",
            "Note", "Product Name", "Quantity", "Price", "Production Status",
            "LinkImg", "LinkThanksCard", "LinkFileDesign", "TimeCreated"
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                    ws.Cell(1, i + 1).Style.Font.Bold = true;
                    ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                int row = 2;
                foreach (var order in orders)
                {
                    if (order.OrderDetails != null && order.OrderDetails.Any())
                    {
                        foreach (var detail in order.OrderDetails)
                        {
                            ws.Cell(row, 1).Value = order.OrderId;
                            ws.Cell(row, 2).Value = order.OrderCode;
                            ws.Cell(row, 3).Value = order.OrderDate.ToString("yyyy-MM-dd HH:mm");
                            ws.Cell(row, 4).Value = order.EndCustomer?.Name ?? "";
                            ws.Cell(row, 5).Value = order.EndCustomer?.Phone ?? "";
                            ws.Cell(row, 6).Value = order.EndCustomer?.Email ?? "";
                            ws.Cell(row, 7).Value = order.EndCustomer?.Address ?? "";
                            ws.Cell(row, 8).Value = order.StatusOrderNavigation?.NameVi ?? "";
                            ws.Cell(row, 9).Value = order.TotalCost ?? 0;
                            ws.Cell(row, 10).Value = ""; // SellerNote (nếu có field thì thay)

                            // ✅ Tên sản phẩm
                            var productName =
                                detail.ProductVariant?.Product?.ProductName ??
                                detail.ProductVariant?.Sku ??
                                "Unnamed Product";
                            ws.Cell(row, 11).Value = productName;

                            ws.Cell(row, 12).Value = detail.Quantity;
                            ws.Cell(row, 13).Value = detail.Price ?? 0;
                            ws.Cell(row, 14).Value = detail.ProductionStatus?.ToString() ?? "";

                            // ✅ Thêm 4 field mới
                            ws.Cell(row, 15).Value = detail.LinkImg ?? "";
                            ws.Cell(row, 16).Value = detail.LinkThanksCard ?? "";
                            ws.Cell(row, 17).Value = detail.LinkFileDesign ?? "";
                            ws.Cell(row, 18).Value = detail.CreatedDate?.ToString("yyyy-MM-dd HH:mm") ?? "";

                            row++;
                        }
                    }
                    else
                    {
                        // Nếu đơn hàng không có chi tiết
                        ws.Cell(row, 1).Value = order.OrderId;
                        ws.Cell(row, 2).Value = order.OrderCode;
                        ws.Cell(row, 3).Value = order.OrderDate.ToString("yyyy-MM-dd HH:mm");
                        ws.Cell(row, 4).Value = order.EndCustomer?.Name ?? "";
                        ws.Cell(row, 5).Value = order.EndCustomer?.Phone ?? "";
                        ws.Cell(row, 6).Value = order.EndCustomer?.Email ?? "";
                        ws.Cell(row, 7).Value = order.EndCustomer?.Address ?? "";
                        ws.Cell(row, 8).Value = order.StatusOrderNavigation?.NameVi ?? "";
                        ws.Cell(row, 9).Value = order.TotalCost ?? 0;
                        ws.Cell(row, 10).Value = "";
                        row++;
                    }
                }

                // ✅ Định dạng đẹp
                ws.Columns().AdjustToContents();
                ws.SheetView.FreezeRows(1);

                using var ms = new MemoryStream();
                workbook.SaveAs(ms);
                ms.Position = 0;

                var fileName = $"AllOrders_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";
                return File(ms.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportAllOrdersToExcel failed");
                return StatusCode(500, "Error exporting all orders");
            }
        }


    }
}