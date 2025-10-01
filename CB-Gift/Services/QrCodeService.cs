using CB_Gift.Data;
using CB_Gift.Services.IService;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

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
            var detail = await _context.OrderDetails
                .Include(od => od.ProductVariant)
                .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

            if (detail == null) return null;

            // Chuỗi data cần encode
            string frontendUrl = $"https://localhost:7015/api/OrderDetail/{orderDetailId}";

            string qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={System.Net.WebUtility.UrlEncode(frontendUrl)}";

            return new
            {
                detail.OrderDetailId,
                ProductName = detail.ProductVariant?.Product?.ProductName ?? "",
                detail.Quantity,
                QrCodeUrl = qrUrl
            };
        }

        public async Task<IEnumerable<object>> GenerateQrCodesAsync(List<int> orderDetailIds)
        {
            var details = await _context.OrderDetails
                .Include(od => od.ProductVariant)
                .Where(od => orderDetailIds.Contains(od.OrderDetailId))
                .ToListAsync();

            return details.Select(detail => new
            {
                detail.OrderDetailId,
                ProductName = detail.ProductVariant?.Product?.ProductName ?? "",
                detail.Quantity,
                QrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=" +
                             System.Net.WebUtility.UrlEncode(
                                 $"OrderDetailId:{detail.OrderDetailId}, Product:{detail.ProductVariant.Product}, Qty:{detail.Quantity}")
            });
        }
    }
}
