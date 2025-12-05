using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Models.Enums;
using CB_Gift.Orders.Import;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CB_Gift.Services
{
    public class OrderFactory
    {
        private readonly ReferenceDataCache _cache;
        private readonly CBGiftDbContext _db;

        public OrderFactory(ReferenceDataCache cache, CBGiftDbContext db)
        {
            _cache = cache;
            _db = db;
        }

        private static string? Clean(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned.Trim();
        }

        // =========================================================================
        // PHƯƠNG THỨC MỚI: Xử lý 1 Group (Nhiều dòng cùng OrderCode)
        // =========================================================================
        public async Task<Order> CreateOrderFromGroupAsync(IGrouping<string, OrderImportRowDto> group, string sellerUserId)
        {
            // 1. Lấy dòng đầu tiên để làm thông tin Header (Khách hàng, Địa chỉ, Ngày tạo...)
            var firstRow = group.First();

            // 2. Xử lý EndCustomer (Dựa trên dòng đầu tiên)
            var endCustomer = await GetOrCreateEndCustomerAsync(firstRow);

            // 3. Tạo Order Header
            var order = new Order
            {
                OrderCode = Clean(firstRow.OrderCode),
                OrderDate = DateTime.UtcNow,
                EndCustomer = endCustomer,
                SellerUserId = sellerUserId,
                CreationDate = DateTime.UtcNow,

                ActiveTts = false,
                ProductionStatus = "Created",
                PaymentStatus = "Unpaid",
                StatusOrder = 1,

                OrderDetails = new List<OrderDetail>()
            };

            // 4. Duyệt qua từng dòng trong Group để tạo OrderDetails
            decimal calculatedTotalCost = 0;

            foreach (var row in group)
            {
                // Tìm Variant cho từng dòng
                var sku = row.SKU.Trim();
                var variant = _cache.ProductVariants.FirstOrDefault(v =>
                    !string.IsNullOrEmpty(v.Sku) &&
                    v.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));

                if (variant == null) continue; // (Lý thuyết đã validate ở Service rồi)

                var unitPrice = variant.TotalCost ?? 0;
                var quantity = row.Quantity > 0 ? row.Quantity : 1;

                var detail = new OrderDetail
                {
                    ProductVariantId = variant.ProductVariantId,
                    Quantity = quantity,
                    Price = unitPrice,

                    Accessory = row.Accessory,
                    Note = row.Note,
                    LinkImg = row.LinkImg,
                    LinkThanksCard = row.LinkThanksCard,
                    LinkFileDesign = row.LinkFileDesign,

                    CreatedDate = DateTime.UtcNow,
                    ProductionStatus = ProductionStatus.DRAFT,
                    NeedDesign = false
                };

                order.OrderDetails.Add(detail);

                // Cộng dồn tiền
                calculatedTotalCost += (unitPrice * quantity);
            }

            // 5. Cập nhật tổng tiền cho Order
            // Nếu Excel có cột TotalCost tổng, bạn có thể lấy firstRow.TotalCost
            // Nhưng tốt nhất là tính tổng từ chi tiết để chính xác.
            order.TotalCost = calculatedTotalCost;

            return order;
        }

        // Tách hàm xử lý Customer cho gọn
        private async Task<EndCustomer> GetOrCreateEndCustomerAsync(OrderImportRowDto dto)
        {
            var normalizedEmail = Clean(dto.Email)?.ToLowerInvariant();
            var normalizedPhone = Clean(dto.Phone);
            EndCustomer? customer = null;

            if (!string.IsNullOrEmpty(normalizedEmail))
                customer = await _db.EndCustomers.FirstOrDefaultAsync(c => c.Email == normalizedEmail);

            if (customer == null && !string.IsNullOrEmpty(normalizedPhone))
                customer = await _db.EndCustomers.FirstOrDefaultAsync(c => c.Phone == normalizedPhone);

            var addressFull = string.IsNullOrEmpty(dto.Ward)
                ? dto.Address
                : $"{dto.Address}, {dto.Ward}";

            if (customer == null)
            {
                customer = new EndCustomer
                {
                    Name = Clean(dto.CustomerName),
                    Phone = normalizedPhone,
                    Email = normalizedEmail,
                    Address = addressFull,
                    ShipState = Clean(dto.Province),
                    ShipCity = Clean(dto.District),
                    ShipCountry = "Vietnam"
                };
                _db.EndCustomers.Add(customer);
            }
            else
            {
                // Update info mới nhất
                customer.Name = Clean(dto.CustomerName);
                customer.Address = addressFull;
                customer.ShipState = Clean(dto.Province);
                customer.ShipCity = Clean(dto.District);
            }
            return customer;
        }

        private string MapPaymentStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return "Unpaid";
            var s = status.Trim().ToLower();
            return s is "paid" or "completed" ? "Paid" :
                   s is "cancelled" ? "Cancelled" : "Pending";
        }
    }
}