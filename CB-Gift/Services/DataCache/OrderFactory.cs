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
        /*  public async Task<Order> CreateOrderFromGroupAsync(IGrouping<string, OrderImportRowDto> group, string sellerUserId)
          {
              // 1. Lấy dòng đầu tiên làm chuẩn
              var firstRow = group.First();

              // 2. Tạo/Lấy EndCustomer (Vẫn lưu text Address, ShipState, ShipCity như cũ)
              var endCustomer = await GetOrCreateEndCustomerAsync(firstRow);

              // =====================================================================
              // LOGIC MỚI: TÌM GHN ID ĐỂ LƯU VÀO ORDER
              // =====================================================================
              int? pId = null;
              int? dId = null;
              string? wCode = null;

              try
              {
                  // A. Tìm ID Tỉnh
                  pId = _cache.FindProvinceId(firstRow.Province);

                  if (pId.HasValue)
                  {
                      // B. Tìm ID Huyện (Chỉ tìm nếu có Tỉnh)
                      dId = await _cache.FindDistrictIdAsync(pId.Value, firstRow.District);

                      if (dId.HasValue)
                      {
                          // C. Tìm Code Xã (Chỉ tìm nếu có Huyện)
                          wCode = await _cache.FindWardCodeAsync(dId.Value, firstRow.Ward);
                      }
                  }
              }
              catch (Exception)
              {
                  // Nếu lỗi kết nối GHN thì bỏ qua, vẫn tạo đơn bình thường nhưng thiếu ID
                  // Bạn có thể log warning ở đây
              }
              // =====================================================================

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

                  // LƯU CÁC ID VỪA TÌM ĐƯỢC VÀO ĐÂY
                  ToProvinceId = pId,
                  ToDistrictId = dId,
                  ToWardCode = wCode,

                  OrderDetails = new List<OrderDetail>()
              };

              // 4. Duyệt qua từng dòng tạo OrderDetails (Giữ nguyên code cũ)
              decimal calculatedTotalCost = 0;
              foreach (var row in group)
              {
                  var sku = row.SKU.Trim();
                  var variant = _cache.ProductVariants.FirstOrDefault(v =>
                      !string.IsNullOrEmpty(v.Sku) &&
                      v.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));

                  if (variant == null) continue;

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
                  calculatedTotalCost += (unitPrice * quantity);
              }

              order.TotalCost = calculatedTotalCost;
              return order;
          }*/

        // =========================================================================
        // PHƯƠNG THỨC MỚI: Xử lý 1 Group (Nhiều dòng cùng OrderCode)
        // =========================================================================
        public async Task<Order> CreateOrderFromGroupAsync(IGrouping<string, OrderImportRowDto> group, string sellerUserId)
        {
            // 1. Lấy dòng đầu tiên làm chuẩn
            var firstRow = group.First();

            // 2. Tạo/Lấy EndCustomer
            var endCustomer = await GetOrCreateEndCustomerAsync(firstRow);

            // ... (Logic tìm Address ID giữ nguyên như bước trước) ...
            int? pId = null; int? dId = null; string? wCode = null;
            try
            {
                pId = _cache.FindProvinceId(firstRow.Province);
                if (pId.HasValue)
                {
                    dId = await _cache.FindDistrictIdAsync(pId.Value, firstRow.District);
                    if (dId.HasValue) wCode = await _cache.FindWardCodeAsync(dId.Value, firstRow.Ward);
                }
            }
            catch { }
            // ...

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
                ToProvinceId = pId,
                ToDistrictId = dId,
                ToWardCode = wCode,
                OrderDetails = new List<OrderDetail>()
            };

            // --- KHỞI TẠO BIẾN TÍNH TOÁN ---
            decimal sumBaseCost = 0;      // Tổng giá Base của tất cả sản phẩm
            decimal maxShipCost = 0;      // Phí Ship cơ bản cao nhất
            decimal maxExtraShip = 0;     // Phí Ship phụ cao nhất
            int totalQuantity = 0;        // Tổng số lượng sản phẩm

            // 4. Duyệt qua từng dòng tạo OrderDetails
            foreach (var row in group)
            {
                var sku = row.SKU.Trim();
                var variant = _cache.ProductVariants.FirstOrDefault(v =>
                    !string.IsNullOrEmpty(v.Sku) &&
                    v.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));

                if (variant == null) continue;

                // Lấy dữ liệu giá từ Variant (handle null bằng 0)
                decimal baseCost = variant.BaseCost ?? 0;
                decimal shipCost = variant.ShipCost ?? 0;
                decimal extraShip = variant.ExtraShipping ?? 0;

                int quantity = row.Quantity > 0 ? row.Quantity : 1;

                // A. Cộng dồn Base Cost
                sumBaseCost += (baseCost * quantity);

                // B. Tìm Max Ship & Max Extra (Dùng để tính TotalShip sau này)
                if (shipCost > maxShipCost) maxShipCost = shipCost;
                if (extraShip > maxExtraShip) maxExtraShip = extraShip;

                // C. Cộng dồn số lượng
                totalQuantity += quantity;

                var detail = new OrderDetail
                {
                    ProductVariantId = variant.ProductVariantId,
                    Quantity = quantity,

                    // LƯU Ý: Lưu Price là BaseCost. 
                    // Vì ShipCost giờ tính gộp ở Order, nếu lưu Price = TotalCost ở đây sẽ bị vênh số liệu.
                    Price = baseCost,

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
            }

            // 5. TÍNH TOÁN FINAL TOTAL COST
            // Công thức: TotalShip = MaxBaseShip + MaxExtraShip * (TotalQuantity - 1)

            // Số lượng tính Extra = Tổng số lượng - 1 (trừ đi sản phẩm chính đã chịu MaxBaseShip)
            // Đảm bảo không âm nếu totalQuantity = 0 (trường hợp hiếm)
            int extraQty = (totalQuantity > 0) ? totalQuantity - 1 : 0;

            decimal totalShipCost = maxShipCost + (maxExtraShip * extraQty);

            // TotalOrder = Tổng Base + Tổng Ship tính được
            order.TotalCost = sumBaseCost + totalShipCost;

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