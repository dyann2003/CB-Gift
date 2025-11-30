using CB_Gift.Data;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;

namespace CB_Gift.Services
{
    public class QrCodeService : IQrCodeService
    {
        private readonly CBGiftDbContext _context;

        public QrCodeService(CBGiftDbContext context)
        {
            _context = context;
        }

        public async Task<object> GenerateQrCodeAsync(int orderDetailId)
        {
            // 1. Lấy thông tin chi tiết hiện tại
            var detail = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductVariant)
                .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (detail == null) return null;

            // 2. Lấy danh sách tất cả các Order Detail trong cùng 1 Order
            // Chỉ lấy OrderDetailId và Quantity để tính toán cho nhẹ DB
            var siblings = await _context.OrderDetails
                .Where(od => od.OrderId == detail.OrderId)
                .OrderBy(od => od.OrderDetailId) // Sắp xếp cố định (ví dụ theo ID tăng dần) để thứ tự không bị nhảy
                .Select(od => new { od.OrderDetailId, od.Quantity })
                .ToListAsync();

            // 3. Tính Tổng số Item (Cộng dồn Quantity của tất cả detail)
            int totalItems = siblings.Sum(s => s.Quantity);

            // 4. Tính Chỉ số bắt đầu (ItemIndex) dựa trên cộng dồn Quantity của các item đứng trước
            int itemIndex = 1;
            foreach (var sib in siblings)
            {
                if (sib.OrderDetailId == orderDetailId)
                {
                    // Đã tìm thấy item hiện tại, dừng cộng
                    break;
                }
                // Nếu chưa tới item hiện tại, cộng dồn số lượng của các item trước đó
                itemIndex += sib.Quantity;
            }

            // 5. Tạo URL QR Code
            string frontendUrl = $"https://cb-gift-fe-sby6.vercel.app/qc/order-detail/{orderDetailId}";
            string qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=400x400&data={System.Net.WebUtility.UrlEncode(frontendUrl)}";
            string orderCode = detail.Order.OrderCode + "_" + totalItems + "IT_" + itemIndex;
            return new
            {
                detail.OrderDetailId,
                orderCode,
                ProductName = detail.ProductVariant?.Product?.ProductName ?? "",
                detail.Quantity,
                QrCodeUrl = qrUrl,

                // Trả về 2 giá trị đã tính toán theo logic mới
                TotalItems = totalItems, // Tổng số lượng sản phẩm vật lý
                ItemIndex = itemIndex    // Số thứ tự bắt đầu của detail này (dựa trên tích lũy quantity)
            };
        }

        public async Task<IEnumerable<object>> GenerateQrCodesAsync(List<int> orderDetailIds)
        {
            var details = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.ProductVariant)
                .ThenInclude(pv => pv.Product) // ✅ BỔ SUNG
                .Where(od => orderDetailIds.Contains(od.OrderDetailId))
                .ToListAsync();

            return details.Select(detail =>
            {
                var productName = detail.ProductVariant?.Product?.ProductName ?? "";
                var raw = $"OrderDetailId:{detail.OrderDetailId}, Product:{productName}, Qty:{detail.Quantity}";
                var url = "https://api.qrserver.com/v1/create-qr-code/?size=400x400&data=" + WebUtility.UrlEncode(raw);

                return new
                {
                    detail.OrderDetailId,
                    detail.Order.OrderCode,
                    ProductName = productName,
                    detail.Quantity,
                    QrCodeUrl = url
                };
            });
        }

    }
}
