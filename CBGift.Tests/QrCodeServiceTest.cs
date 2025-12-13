using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CB_Gift.Data;
using CB_Gift.Models;
using CB_Gift.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class QrCodeServiceTests
    {
        private static CBGiftDbContext BuildContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .EnableSensitiveDataLogging()
                .Options;

            var ctx = new CBGiftDbContext(options);
            return ctx;
        }

        private static async Task SeedAsync(CBGiftDbContext ctx)
        {
     
            var product = new Product { ProductId = 10, ProductName = "Mug Premium" };
            var variant = new ProductVariant { ProductVariantId = 100, ProductId = 10, Product = product, Sku = "MUG-PRM" };

            var order = new Order { OrderId = 1000, OrderCode = "ORD-001", SellerUserId = "seller-001" };

            var od1 = new OrderDetail
            {
                OrderDetailId = 5001,
                OrderId = 1000,
                Order = order,
                ProductVariantId = 100,
                ProductVariant = variant,
                Quantity = 2
            };

            var od2 = new OrderDetail
            {
                OrderDetailId = 5002,
                OrderId = 1000,
                Order = order,
                ProductVariantId = 100,
                ProductVariant = variant,
                Quantity = 3
            };

            await ctx.Products.AddAsync(product);
            await ctx.ProductVariants.AddAsync(variant);
            await ctx.Orders.AddAsync(order);
            await ctx.OrderDetails.AddRangeAsync(od1, od2);
            await ctx.SaveChangesAsync();
        }

        [Fact]
        public async Task GenerateQrCodeAsync_ReturnsNull_WhenOrderDetailNotFound()
        {
            var ctx = BuildContext(nameof(GenerateQrCodeAsync_ReturnsNull_WhenOrderDetailNotFound));
            await SeedAsync(ctx);

            var svc = new QrCodeService(ctx);

            var result = await svc.GenerateQrCodeAsync(orderDetailId: 999999);

            Assert.Null(result);
        }




      
        [Fact]
        public async Task GenerateQrCodesAsync_ReturnsOk_AndQrContainsEncodedProductName()
        {
            var ctx = BuildContext(nameof(GenerateQrCodesAsync_ReturnsOk_AndQrContainsEncodedProductName));
            await SeedAsync(ctx);

            var svc = new QrCodeService(ctx);
            var results = (await svc.GenerateQrCodesAsync(new List<int> { 5001, 5002 })).ToList();

            Assert.Equal(2, results.Count);

            foreach (var r in results)
            {
                var t = r.GetType();
                int id = (int)t.GetProperty("OrderDetailId")!.GetValue(r)!;
                string productName = (string)t.GetProperty("ProductName")!.GetValue(r)!;
                int qty = (int)t.GetProperty("Quantity")!.GetValue(r)!;
                string qrUrl = (string)t.GetProperty("QrCodeUrl")!.GetValue(r)!;

                Assert.False(string.IsNullOrWhiteSpace(productName));

                var expectedRaw = $"OrderDetailId:{id}, Product:{productName}, Qty:{qty}";
                var expectedEncoded = WebUtility.UrlEncode(expectedRaw);
                Assert.Contains(expectedEncoded, qrUrl);
            }
        }
    }
}
