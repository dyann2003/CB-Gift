using CB_Gift.Data;
using CB_Gift.Models;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using CB_Gift.DTOs;
using CB_Gift.Services.IService;
using DocumentFormat.OpenXml.InkML;

namespace CB_Gift.Orders.Import
{
    public class OrderImportService : IOrderImportService
    {
        private readonly ReferenceDataCache _cache;
        private readonly CBGiftDbContext _db;
        private readonly IOrderService _orderService; // existing service (MakeOrder)
        private readonly IValidator<OrderImportRowDto> _validator;
        private readonly ILogger<OrderImportService> _logger;

        public OrderImportService(
            ReferenceDataCache cache,
            CBGiftDbContext db,
            IOrderService orderService,
            IValidator<OrderImportRowDto> validator,
            ILogger<OrderImportService> logger)
        {
            _cache = cache;
            _db = db;
            _orderService = orderService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<byte[]> GenerateExcelTemplateAsync()
        {
            // 1. LẤY DATA: Lấy danh sách SKU từ ProductVariant
            // Điều kiện: Product cha có Status != 0 (Giả sử 0 là ẩn/xóa) và Sku không rỗng
            var skuList = await _db.ProductVariants
                .Include(pv => pv.Product) // Join sang bảng Product để check Status
                .Where(pv => pv.Product.Status != 0 && !string.IsNullOrEmpty(pv.Sku))
                .Select(pv => pv.Sku)
                .Distinct() // Loại bỏ SKU trùng lặp (nếu có)
                .OrderBy(s => s) // Sắp xếp A-Z cho dễ tìm
                .ToListAsync();

            using var wb = new XLWorkbook();

            // --- SHEET 1: GIAO DIỆN NGƯỜI DÙNG (OrdersTemplate) ---
            var ws = wb.Worksheets.Add("OrdersTemplate");

            // Định nghĩa Header
            var headers = new[]
            {
                "OrderCode",      // Col 1
                "CustomerName",   // Col 2
                "Phone",          // Col 3
                "Email",          // Col 4
                "Address",        // Col 5
                "Province",       // Col 6
                "District",       // Col 7
                "Ward",           // Col 8
                "Sku",            // Col 9  <-- Cột cần Dropdown
                "Quantity",       // Col 10
                "Accessory",      // Col 11
                "Note",           // Col 12
                "LinkImg",        // Col 13
                "LinkThanksCard", // Col 14
                "LinkFileDesign"  // Col 15
            };

            // Điền Header vào dòng 1
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
            }

            // Format Header cho đẹp
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFD700"); // Màu vàng Gold
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // Freeze dòng header để khi cuộn xuống vẫn thấy tiêu đề
            ws.SheetView.FreezeRows(1);


            // --- SHEET 2: DỮ LIỆU NGUỒN CHO DROPDOWN (RefData) ---
            // Tạo sheet mới để chứa danh sách SKU
            var wsData = wb.Worksheets.Add("RefData");

            if (skuList.Any())
            {
                // Đổ dữ liệu SKU vào cột A của sheet RefData
                for (int i = 0; i < skuList.Count; i++)
                {
                    wsData.Cell(i + 1, 1).Value = skuList[i];
                }

                // --- CẤU HÌNH DATA VALIDATION (DROPDOWN) ---
                // Cột Sku là cột số 9 (I) ở Sheet OrdersTemplate
                int skuColumnIndex = 9;
                int maxRows = 5000; // Áp dụng cho 5000 dòng đầu tiên

                // Chọn vùng cần tạo dropdown (Từ dòng 2 đến 5000 tại cột 9)
                var validationRange = ws.Range(2, skuColumnIndex, maxRows, skuColumnIndex);

                // Công thức tham chiếu: =RefData!$A$1:$A${số_lượng_sku}
                // Dùng tham chiếu range thay vì list string cứng để không bị giới hạn ký tự của Excel
                var sourceRangeFormula = $"=RefData!$A$1:$A${skuList.Count}";

                // Set Validation
                validationRange.SetDataValidation().List(sourceRangeFormula, true);

                // Thêm thông báo lỗi khi người dùng nhập sai SKU
                var validation = validationRange.GetDataValidation();
                validation.ErrorStyle = XLErrorStyle.Stop;
                validation.ErrorTitle = "Sai mã SKU";
                validation.ErrorMessage = "SKU này không tồn tại trong hệ thống. Vui lòng chọn từ danh sách.";
                validation.InputTitle = "Chọn SKU";
                validation.InputMessage = "Vui lòng chọn mã sản phẩm từ danh sách xổ xuống.";
            }

            // Ẩn sheet RefData đi để người dùng không sửa bậy
            wsData.Visibility = XLWorksheetVisibility.Hidden;


            // --- HOÀN THIỆN ---
            // Tự động căn chỉnh độ rộng cột theo nội dung
            ws.Columns().AdjustToContents();

            // Set độ rộng cố định cho một số cột dài để nhìn đẹp hơn
            ws.Column(5).Width = 30; // Address
            ws.Column(9).Width = 20; // SKU
            ws.Column(12).Width = 25; // Note

            // Xuất file ra byte array
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<OrderImportResult> ImportFromExcelAsync(IFormFile file, string sellerUserId)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("File empty", nameof(file));

            // Ensure reference cache loaded
            await _cache.LoadAsync();

            var result = new OrderImportResult();
            var errors = new List<OrderImportRowError>();
            var parsedRows = new List<OrderImportRowDto>();

            using var stream = file.OpenReadStream();
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();

            var used = ws.RangeUsed();
            if (used == null) throw new Exception("Không tìm thấy dữ liệu trên sheet.");

            var rows = ws.RowsUsed().Skip(1); // skip header
            result.TotalRows = rows.Count();

            // 1) Parse + per-row validate
            foreach (var r in rows)
            {
                var dto = new OrderImportRowDto
                {
                    RowNumber = r.RowNumber(),
                    OrderCode = r.Cell(1).GetString().Trim(),
                    CustomerName = r.Cell(2).GetString().Trim(),
                    Phone = r.Cell(3).GetString().Trim(),
                    Email = Clean(r.Cell(4).GetString()),
                    Address = r.Cell(5).GetString().Trim(),
                    Province = r.Cell(6).GetString().Trim(),
                    District = r.Cell(7).GetString().Trim(),
                    Ward = r.Cell(8).GetString().Trim(),
                    SKU = r.Cell(9).GetString().Trim(),
                    Quantity = GetIntSafe(r.Cell(10), 1),
                    Accessory = r.Cell(11).GetString().Trim(),
                    Note = r.Cell(12).GetString().Trim(),
                    LinkImg = r.Cell(13).GetString().Trim(),
                    LinkThanksCard = r.Cell(14).GetString().Trim(),
                    LinkFileDesign = r.Cell(15).GetString().Trim()
                };

                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    errors.Add(new OrderImportRowError
                    {
                        RowNumber = dto.RowNumber,
                        Messages = validation.Errors.Select(e => e.ErrorMessage).ToList()
                    });
                    _logger.LogWarning("Import row {Row} validation failed: {Errors}", dto.RowNumber,
                        string.Join(";", validation.Errors.Select(e => e.ErrorMessage)));
                    continue;
                }

                parsedRows.Add(dto);
            }

            // 2) Group by OrderCode
            var groups = parsedRows.GroupBy(x => x.OrderCode.Trim(), StringComparer.OrdinalIgnoreCase);

            // 3) For each group -> Build MakeOrderDto and call _orderService.MakeOrder(...)
            // Use DB transaction to prevent partial failure of multiple groups if you prefer. Here we commit per group (MakeOrder likely persists).
            foreach (var group in groups)
            {
                var head = group.First();

                // Build MakeOrderDto using your existing DTO classes (MakeOrderDto, EndCustomerCreateRequest, OrderCreateRequest, OrderDetailCreateRequest)
                var makeDto = new MakeOrderDto
                {
                    CustomerInfo = new EndCustomerCreateRequest
                    {
                        Name = head.CustomerName,
                        Phone = head.Phone,
                        Email = head.Email,
                        Address = head.Address,
                        ShipState = head.Province,
                        ShipCity = head.District,
                        ShipCountry = "Vietnam",
                        ZipCode = null
                    },
                    OrderCreate = new OrderCreateRequest
                    {
                        OrderCode = head.OrderCode,
                        OrderDate = DateTime.UtcNow,
                        TotalCost = null,
                        ActiveTTS = false,
                        ProductionStatus = "Created",
                        PaymentStatus = "Unpaid",
                        StatusOrder = 1
                    },
                    OrderDetails = new List<OrderDetailCreateRequest>()
                };

                foreach (var line in group)
                {
                    var variant = _cache.ProductVariants
                        .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Sku) &&
                                             v.Sku.Equals(line.SKU, StringComparison.OrdinalIgnoreCase));

                    if (variant == null)
                    {
                        errors.Add(new OrderImportRowError
                        {
                            RowNumber = line.RowNumber,
                            Messages = new List<string> { $"SKU '{line.SKU}' không tìm thấy." }
                        });
                        _logger.LogWarning("Import: SKU not found at row {Row} SKU={SKU}", line.RowNumber, line.SKU);
                        continue;
                    }

                    var detail = new OrderDetailCreateRequest
                    {
                        ProductVariantID = variant.ProductVariantId,
                        Quantity = line.Quantity,
                        NeedDesign = false,
                        LinkImg = string.IsNullOrWhiteSpace(line.LinkImg) ? null : line.LinkImg,
                        LinkThanksCard = string.IsNullOrWhiteSpace(line.LinkThanksCard) ? null : line.LinkThanksCard,
                        LinkDesign = string.IsNullOrWhiteSpace(line.LinkFileDesign) ? null : line.LinkFileDesign,
                        Accessory = line.Accessory,
                        Note = line.Note,
                        Price = variant.BaseCost // Use DB base cost as source of truth
                    };

                    makeDto.OrderDetails!.Add(detail);
                }

                if (makeDto.OrderDetails == null || makeDto.OrderDetails.Count == 0)
                {
                    _logger.LogInformation("Import: skip OrderCode {OrderCode} because it has no valid details.", head.OrderCode);
                    continue;
                }

                try
                {
                    // call existing MakeOrder method
                    // signature in your project: Task<MakeOrderResponse> MakeOrder(MakeOrderDto request, string sellerUserId)
                    var resp = await _orderService.MakeOrder(makeDto, sellerUserId);
                    result.SuccessCount++;
                    _logger.LogInformation("Import: created Order {OrderCode} -> id {OrderId}", head.OrderCode, resp.OrderId);
                }
                catch (Exception ex)
                {
                    errors.Add(new OrderImportRowError
                    {
                        RowNumber = head.RowNumber,
                        Messages = new List<string> { $"Lỗi tạo Order '{head.OrderCode}': {ex.Message}" }
                    });
                    _logger.LogError(ex, "Import: error creating order {OrderCode}", head.OrderCode);
                }
            }

            result.Errors = errors;
            var errorBytes = errors.Any() ? CreateErrorReport(ws, errors) : null;

            result.ErrorReportFileBytes = errorBytes;
            result.ErrorReportFileName = errors.Any() ? $"order-import-errors-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx" : null;

            return result;
        }


        private byte[] CreateErrorReport(IXLWorksheet sourceSheet, List<OrderImportRowError> errors)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Errors");

            // Copy headers (if exist)
            var headerRow = sourceSheet.Row(1);
            var lastCol = sourceSheet.RangeUsed().ColumnCount();
            for (int c = 1; c <= lastCol; c++)
            {
                ws.Cell(1, c).Value = headerRow.Cell(c).Value;
            }
            ws.Cell(1, lastCol + 1).Value = "__ERRORS__";
            ws.Row(1).Style.Font.Bold = true;

            // For each error, copy source row and append error message (if source row exists)
            int outRow = 2;
            foreach (var err in errors)
            {
                var srcRow = sourceSheet.Row(err.RowNumber);
                if (srcRow != null && !srcRow.IsEmpty())
                {
                    for (int c = 1; c <= lastCol; c++)
                    {
                        ws.Cell(outRow, c).Value = srcRow.Cell(c).Value;
                    }
                }
                ws.Cell(outRow, lastCol + 1).Value = string.Join("; ", err.Messages);
                outRow++;
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        #region Helpers
        private static int GetIntSafe(IXLCell cell, int defaultValue = 0)
        {
            if (cell == null || cell.IsEmpty()) return defaultValue;
            if (cell.DataType == XLDataType.Number) return (int)cell.GetDouble();
            var text = cell.GetString().Trim();
            if (int.TryParse(text, out var v)) return v;
            return defaultValue;
        }

        private static string? Clean(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }
        #endregion
    }
}
