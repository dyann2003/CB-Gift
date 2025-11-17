using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Orders.Import;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CB_Gift.Services
{
    /// <summary>
    /// Chịu trách nhiệm map từ OrderImportRowDto -> Order (kèm EndCustomer + OrderDetail)
    /// </summary>
    public class OrderFactory
    {
        private readonly ReferenceDataCache _cache;
        private readonly CBGiftDbContext _db; // hiện chưa dùng, nhưng để sẵn nếu sau này cần lookup

        public OrderFactory(ReferenceDataCache cache, CBGiftDbContext db)
        {
            _cache = cache;
            _db = db;
        }

        /// <summary>
        /// Tạo entity Order hoàn chỉnh từ 1 dòng Excel (OrderImportRowDto) + seller hiện tại.
        /// </summary>
        /// 
        private static string? Clean(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            // Loại mọi whitespace unicode
            var cleaned = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "");

            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned.Trim();
        }
        private string NormalizeSize(string? size)
        {
            if (string.IsNullOrWhiteSpace(size))
                return string.Empty;

            var s = size.Trim().ToLower();

            // bỏ chữ "inch", "in" nếu có
            s = s.Replace("inch", "").Replace("in", "").Trim();

            // parse số: xử lý 4, 4.0, 4.00, 4,0 ...
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            {
                return val.ToString("0.##", CultureInfo.InvariantCulture); // 4.00 -> "4", 5.90 -> "5.9"
            }

            return s;
        }
        public async Task<Order> CreateOrderEntityAsync(OrderImportRowDto dto, string sellerUserId)
        {
            
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(sellerUserId))
                throw new ArgumentException("SellerUserId không hợp lệ.", nameof(sellerUserId));
            var normalizedEmail = Clean(dto.Email)?.ToLowerInvariant();
            EndCustomer endCustomer;
            // 1️⃣ Tìm Product theo ProductName
            var product = _cache.Products.FirstOrDefault(p =>
                p.ProductName.Equals(dto.ProductName!.Trim(), StringComparison.OrdinalIgnoreCase));

            if (product == null)
                throw new Exception($"Product '{dto.ProductName}' không tồn tại (Row {dto.RowNumber}).");

            // 2️⃣ Tìm ProductVariant theo ProductId + SizeInch
            var dtoSize = NormalizeSize(dto.SizeInch);

            var variant = _cache.ProductVariants
                .Where(v => v.ProductId == product.ProductId)
                .FirstOrDefault(v => NormalizeSize(v.SizeInch) == dtoSize);

            if (variant == null)
                throw new Exception(
                    $"Không tìm thấy ProductVariant cho Product = {product.ProductName}, Size = {dto.SizeInch} (Row {dto.RowNumber}).");
            var unitPrice = variant.TotalCost ?? 0m;
            // 3️⃣ Tạo EndCustomer từ dữ liệu Excel
            var email = Clean(dto.Email)?.ToLowerInvariant();  // email nên lower luôn

            if (!string.IsNullOrEmpty(normalizedEmail))
            {
                // 2️⃣ Tìm EndCustomer theo email
                endCustomer = await _db.EndCustomers
                    .FirstOrDefaultAsync(c => c.Email != null &&
                                              c.Email.ToLower() == normalizedEmail);

                // 3️⃣ Nếu chưa có thì tạo mới
                if (endCustomer == null)
                {
                    endCustomer = new EndCustomer
                    {
                        Name = Clean(dto.CustomerName),
                        Phone = Clean(dto.Phone),
                        Email = normalizedEmail,
                        Address = Clean(dto.Address),
                        ShipState = Clean(dto.ShipState),
                        ShipCity = Clean(dto.ShipCity),
                        ShipCountry = Clean(dto.ShipCountry),
                        Zipcode = Clean(dto.Zipcode)
                    };

                    _db.EndCustomers.Add(endCustomer);
                }
            }
            else
            {
                // Không có email → bạn có thể quyết định logic riêng (tạo mới luôn
                // hoặc tìm theo Name + Phone…)
                endCustomer = new EndCustomer
                {
                    Name = Clean(dto.CustomerName),
                    Phone = Clean(dto.Phone),
                    Address = Clean(dto.Address),
                    ShipState = Clean(dto.ShipState),
                    ShipCity = Clean(dto.ShipCity),
                    ShipCountry = Clean(dto.ShipCountry),
                    Zipcode = Clean(dto.Zipcode)
                };

                _db.EndCustomers.Add(endCustomer);
            }

            // 4️⃣ Tạo Order chính
            var order = new Order
            {
                OrderCode = dto.OrderCode ?? string.Empty,
                OrderDate = dto.OrderDate ?? DateTime.UtcNow,

                // dùng navigation EndCustomer, EF sẽ tự insert và set EndCustomerId
                EndCustomer = endCustomer,
                SellerUserId = sellerUserId,

                CreationDate = dto.TimeCreated ?? DateTime.UtcNow,
                CostScan = null, // nếu sau này Excel có cột CostScan thì map thêm

                ActiveTts = dto.ActiveTTS,

                TotalCost = dto.TotalCost ?? dto.TotalAmount,

                ProductionStatus = "DRAFT", // default workflow
                PaymentStatus = MapPaymentStatus(dto.PaymentStatus),
                Tracking = null, // nếu Excel có cột Tracking thì map thêm

                StatusOrder = dto.StatusOrder,

                OrderDetails = new List<OrderDetail>()
            };

            var detail = new OrderDetail
            {
                ProductVariantId = variant.ProductVariantId,

                LinkImg = dto.LinkImg,
                LinkThanksCard = dto.LinkThanksCard,
                LinkFileDesign = dto.LinkFileDesign,
                Accessory = dto.Accessory,
                Note = dto.OrderNotes,

                // 👉 dùng Quantity từ file Excel, fallback = 1
                Quantity = dto.Quantity > 0 ? dto.Quantity : 1,
                CreatedDate = DateTime.UtcNow,

                ProductionStatus = Models.Enums.ProductionStatus.DRAFT,
                NeedDesign = false,

                AssignedDesignerUserId = null,
                AssignedAt = null,

                // 👉 Price lấy theo ProductVariant trong DB
                Price = unitPrice
            };


            order.OrderDetails.Add(detail);
            
            return order;
        }

        /// <summary>
        /// Chuẩn hóa PaymentStatus từ dữ liệu Excel sang giá trị lưu trong DB (string).
        /// </summary>
        private string MapPaymentStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "Pending";

            var s = status.Trim().ToLower();

            return s switch
            {
                "paid" or "completed" => "Paid",
                "pending" => "Pending",
                "unpaid" => "Unpaid",
                "refunded" => "Refunded",
                "cancelled" or "canceled" => "Cancelled",
                _ => "Pending"
            };
        }
    }
}
