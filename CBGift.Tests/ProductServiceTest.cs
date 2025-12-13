using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CB_Gift.Tests.Services
{
    public class ProductServiceTests
    {
        private static CBGiftDbContext BuildContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CBGiftDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .EnableSensitiveDataLogging()
                .Options;

            return new CBGiftDbContext(options);
        }

        private static Mock<IMapper> BuildMapper()
        {
            var mapper = new Mock<IMapper>();

            // ProductCreateDto -> Product
            mapper.Setup(m => m.Map<Product>(It.IsAny<ProductCreateDto>()))
                  .Returns((ProductCreateDto dto) => new Product
                  {
                      ProductName = dto.ProductName,
                      ProductCode = dto.ProductCode,
                      CategoryId = dto.CategoryId,
                      Status = dto.Status,
                      ItemLink = dto.ItemLink,
                      Describe = dto.Describe,
                      Template = dto.Template,
                      ProductVariants = new List<ProductVariant>(),
                      Tags = new List<Tag>()
                  });

            // Variant Create/Update -> ProductVariant
            mapper.Setup(m => m.Map<ProductVariant>(It.IsAny<ProductVariantCreateDto>()))
                  .Returns((ProductVariantCreateDto d) => new ProductVariant
                  {
                      // id sẽ do DB gán
                      LengthCm = d.LengthCm,
                      HeightCm = d.HeightCm,
                      WidthCm = d.WidthCm,
                      WeightGram = d.WeightGram,
                      ShipCost = d.ShipCost,
                      BaseCost = d.BaseCost,
                      ThicknessMm = d.ThicknessMm,
                      SizeInch = d.SizeInch,
                      Layer = d.Layer,
                      CustomShape = d.CustomShape,
                      Sku = d.Sku,
                      ExtraShipping = d.ExtraShipping,
                      TotalCost = d.TotalCost
                  });

            mapper.Setup(m => m.Map<ProductVariant>(It.IsAny<ProductVariantUpdateDto>()))
                  .Returns((ProductVariantUpdateDto d) => new ProductVariant
                  {
                      ProductVariantId = d.ProductVariantId,
                      LengthCm = d.LengthCm,
                      HeightCm = d.HeightCm,
                      WidthCm = d.WidthCm,
                      WeightGram = d.WeightGram,
                      ShipCost = d.ShipCost,
                      BaseCost = d.BaseCost,
                      ThicknessMm = d.ThicknessMm,
                      SizeInch = d.SizeInch,
                      Layer = d.Layer,
                      CustomShape = d.CustomShape,
                      Sku = d.Sku,
                      ExtraShipping = d.ExtraShipping,
                      TotalCost = d.TotalCost
                  });

            // Map ProductUpdateDto -> existing Product (chỉ scalar; variants xử lý trong service)
            mapper.Setup(m => m.Map(It.IsAny<ProductUpdateDto>(), It.IsAny<Product>()))
                  .Callback((ProductUpdateDto dto, Product p) =>
                  {
                      if (dto.ProductName != null) p.ProductName = dto.ProductName;
                      if (dto.ProductCode != null) p.ProductCode = dto.ProductCode;
                      if (dto.CategoryId.HasValue) p.CategoryId = dto.CategoryId.Value;
                      if (dto.Describe != null) p.Describe = dto.Describe;
                      if (dto.ItemLink != null) p.ItemLink = dto.ItemLink;
                      if (dto.Template != null) p.Template = dto.Template;
                      if (dto.Status.HasValue) p.Status = dto.Status;
                  });

            // Map ProductVariantUpdateDto -> existing variant
            mapper.Setup(m => m.Map(It.IsAny<ProductVariantUpdateDto>(), It.IsAny<ProductVariant>()))
                  .Callback((ProductVariantUpdateDto d, ProductVariant v) =>
                  {
                      v.LengthCm = d.LengthCm;
                      v.HeightCm = d.HeightCm;
                      v.WidthCm = d.WidthCm;
                      v.WeightGram = d.WeightGram;
                      v.ShipCost = d.ShipCost;
                      v.BaseCost = d.BaseCost;
                      v.ThicknessMm = d.ThicknessMm;
                      v.SizeInch = d.SizeInch;
                      v.Layer = d.Layer;
                      v.CustomShape = d.CustomShape;
                      v.Sku = d.Sku;
                      v.ExtraShipping = d.ExtraShipping;
                      v.TotalCost = d.TotalCost;
                  });

            // Product -> ProductDto
            mapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
                  .Returns((Product p) => new ProductDto
                  {
                      ProductId = p.ProductId,
                      ProductName = p.ProductName,
                      ProductCode = p.ProductCode,
                      CategoryId = p.CategoryId,
                      CategoryName = p.Category?.CategoryName,
                      Describe = p.Describe,
                      ItemLink = p.ItemLink,
                      Template = p.Template,
                      Status = p.Status,
                      TagIds = p.Tags?.Select(t => t.TagsId).ToList(),
                      Variants = p.ProductVariants?.Select(v => new ProductVariantDto
                      {
                          ProductVariantId = v.ProductVariantId,
                          LengthCm = v.LengthCm,
                          HeightCm = v.HeightCm,
                          WidthCm = v.WidthCm,
                          WeightGram = v.WeightGram,
                          ShipCost = v.ShipCost,
                          BaseCost = v.BaseCost,
                          ThicknessMm = v.ThicknessMm,
                          SizeInch = v.SizeInch,
                          Layer = v.Layer,
                          CustomShape = v.CustomShape,
                          Sku = v.Sku,
                          ExtraShipping = v.ExtraShipping,
                          TotalCost = v.TotalCost
                      }).ToList()
                  });

            // IEnumerable<Product> -> IEnumerable<ProductDto>
            mapper.Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
                  .Returns((IEnumerable<Product> list) => list.Select(p => new ProductDto
                  {
                      ProductId = p.ProductId,
                      ProductName = p.ProductName,
                      ProductCode = p.ProductCode,
                      CategoryId = p.CategoryId,
                      CategoryName = p.Category?.CategoryName,
                      Describe = p.Describe,
                      ItemLink = p.ItemLink,
                      Template = p.Template,
                      Status = p.Status,
                      TagIds = p.Tags?.Select(t => t.TagsId).ToList(),
                      Variants = p.ProductVariants?.Select(v => new ProductVariantDto
                      {
                          ProductVariantId = v.ProductVariantId,
                          LengthCm = v.LengthCm,
                          HeightCm = v.HeightCm,
                          WidthCm = v.WidthCm,
                          WeightGram = v.WeightGram,
                          ShipCost = v.ShipCost,
                          BaseCost = v.BaseCost,
                          ThicknessMm = v.ThicknessMm,
                          SizeInch = v.SizeInch,
                          Layer = v.Layer,
                          CustomShape = v.CustomShape,
                          Sku = v.Sku,
                          ExtraShipping = v.ExtraShipping,
                          TotalCost = v.TotalCost
                      }).ToList()
                  }).ToList());

            return mapper;
        }

        private static async Task SeedBasicAsync(CBGiftDbContext ctx)
        {
            // Category
            ctx.Categories.AddRange(
                new Category { CategoryId = 1, CategoryName = "Mugs" },
                new Category { CategoryId = 2, CategoryName = "Shirts" }
            );

            // Tags
            ctx.Tags.AddRange(
                new Tag { TagsId = 1, TagName = "Hot" },
                new Tag { TagsId = 2, TagName = "New" },
                new Tag { TagsId = 3, TagName = "Sale" }
            );

            // Products + Variants
            var p1 = new Product
            {
                ProductId = 1,
                ProductName = "Red Mug",
                ProductCode = "RMUG",
                Status = 1,
                CategoryId = 1,
                Category = await ctx.Categories.FindAsync(1),
                Describe = "Desc 1",
                ProductVariants = new List<ProductVariant>
                {
                    new ProductVariant { ProductVariantId = 10, Sku = "RMUG-S", BaseCost = 9.9m, TotalCost = 12.9m },
                    new ProductVariant { ProductVariantId = 11, Sku = "RMUG-L", BaseCost = 11.9m, TotalCost = 14.9m }
                },
                Tags = new List<Tag>()
            };

            var p2 = new Product
            {
                ProductId = 2,
                ProductName = "Blue Shirt",
                ProductCode = "BSHIRT",
                Status = 0,
                CategoryId = 2,
                Category = await ctx.Categories.FindAsync(2),
                Describe = "Desc 2",
                ProductVariants = new List<ProductVariant>
                {
                    new ProductVariant { ProductVariantId = 20, Sku = "BS-M", BaseCost = 19.9m, TotalCost = 24.9m }
                },
                Tags = new List<Tag>()
            };

            ctx.Products.AddRange(p1, p2);
            await ctx.SaveChangesAsync();
        }

    

        [Fact]
        public async Task GetAllProductsHaveStatusTrueAsync_Filters_Status_1()
        {
            var db = BuildContext(nameof(GetAllProductsHaveStatusTrueAsync_Filters_Status_1));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var result = await svc.GetAllProductsHaveStatusTrueAsync();

            Assert.Single(result);
            Assert.Equal("Red Mug", result.First().ProductName);
        }

        [Fact]
        public async Task GetHiddenProductsAsync_Filters_Status_0()
        {
            var db = BuildContext(nameof(GetHiddenProductsAsync_Filters_Status_0));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var result = await svc.GetHiddenProductsAsync();

            Assert.Single(result);
            Assert.Equal("Blue Shirt", result.First().ProductName);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_Null_When_NotFound()
        {
            var db = BuildContext(nameof(GetByIdAsync_Returns_Null_When_NotFound));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var r = await svc.GetByIdAsync(999);
            Assert.Null(r);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_Product_WithVariants_And_CategoryName()
        {
            var db = BuildContext(nameof(GetByIdAsync_Returns_Product_WithVariants_And_CategoryName));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var r = await svc.GetByIdAsync(1);

            Assert.NotNull(r);
            Assert.Equal("Red Mug", r!.ProductName);
            Assert.Equal("Mugs", r.CategoryName);
            Assert.Equal(2, r.Variants!.Count);
            Assert.Contains(r.Variants!, v => v.Sku == "RMUG-L" && v.BaseCost == 11.9m && v.TotalCost == 14.9m);
        }

        [Fact]
        public async Task CreateAsync_Creates_Product_With_Tags_And_Variants()
        {
            var db = BuildContext(nameof(CreateAsync_Creates_Product_With_Tags_And_Variants));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var create = new ProductCreateDto
            {
                ProductName = "Black Mug",
                ProductCode = "BMUG",
                Status = 1,
                CategoryId = 1,
                Describe = "New Desc",
                TagIds = new List<int> { 1, 3 },
                Variants = new List<ProductVariantCreateDto>
                {
                    new ProductVariantCreateDto { Sku = "BMUG-OS", BaseCost = 15.5m, TotalCost = 18.0m }
                }
            };

            var created = await svc.CreateAsync(create);

            var inDb = await db.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.ProductCode == "BMUG");

            Assert.NotNull(inDb);
            Assert.Equal("Black Mug", inDb!.ProductName);
            Assert.Equal(1, inDb.Status);
            Assert.Equal(1, inDb.ProductVariants.Count);
            Assert.Equal("BMUG-OS", inDb.ProductVariants.First().Sku);
            Assert.Equal(2, inDb.Tags.Count); // tagIds 1 & 3
            // DTO returned
            Assert.Equal("Black Mug", created.ProductName);
            Assert.Single(created.Variants!);
            Assert.Equal("BMUG-OS", created.Variants![0].Sku);
        }

        [Fact]
        public async Task UpdateAsync_Updates_Product_Adds_Updates_And_Removes_Variants()
        {
            var db = BuildContext(nameof(UpdateAsync_Updates_Product_Adds_Updates_And_Removes_Variants));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            // Product 1 has variants: 10 (RMUG-S), 11 (RMUG-L)
            var req = new ProductUpdateDto
            {
                ProductName = "Red Mug V2",
                ProductCode = "RMUG-V2",
                CategoryId = 1,
                Status = 1,
                Variants = new List<ProductVariantUpdateDto>
                {
                    // Update 10
                    new ProductVariantUpdateDto
                    {
                        ProductVariantId = 10,
                        Sku = "RMUG-S+",
                        BaseCost = 10.9m,
                        TotalCost = 13.5m
                    },
                    // Add new (id = 0)
                    new ProductVariantUpdateDto
                    {
                        ProductVariantId = 0,
                        Sku = "RMUG-M",
                        BaseCost = 11.9m,
                        TotalCost = 14.2m
                    }
                    // Variant 11 bị remove do không gửi lên
                }
            };

            var updated = await svc.UpdateAsync(1, req);

            var inDb = await db.Products.Include(p => p.ProductVariants)
                                        .FirstAsync(p => p.ProductId == 1);

            Assert.Equal("Red Mug V2", updated.ProductName);
            Assert.Equal("RMUG-V2", updated.ProductCode);
            Assert.Equal(2, updated.Variants!.Count);

            // Đã update 10
            Assert.Contains(inDb.ProductVariants, v => v.ProductVariantId == 10 && v.Sku == "RMUG-S+" && v.BaseCost == 10.9m && v.TotalCost == 13.5m);
            // Đã add mới
            Assert.Contains(inDb.ProductVariants, v => v.Sku == "RMUG-M" && v.BaseCost == 11.9m && v.TotalCost == 14.2m);
            // Đã remove 11
            Assert.DoesNotContain(inDb.ProductVariants, v => v.ProductVariantId == 11);
        }

        [Fact]
        public async Task UpdateAsync_Throws_When_Product_NotFound()
        {
            var db = BuildContext(nameof(UpdateAsync_Throws_When_Product_NotFound));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var req = new ProductUpdateDto { ProductName = "X" };

            await Assert.ThrowsAsync<Exception>(() => svc.UpdateAsync(999, req));
        }

        [Fact]
        public async Task DeleteAsync_Returns_False_When_NotFound()
        {
            var db = BuildContext(nameof(DeleteAsync_Returns_False_When_NotFound));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var ok = await svc.DeleteAsync(999);

            Assert.False(ok);
        }

        [Fact]
        public async Task DeleteAsync_Removes_Product()
        {
            var db = BuildContext(nameof(DeleteAsync_Removes_Product));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var ok = await svc.DeleteAsync(2);

            Assert.True(ok);
            Assert.Null(await db.Products.FindAsync(2));
        }

        [Fact]
        public async Task SoftDeleteProductAsync_Sets_Status_0()
        {
            var db = BuildContext(nameof(SoftDeleteProductAsync_Sets_Status_0));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var ok = await svc.SoftDeleteProductAsync(1);

            Assert.True(ok);
            var p = await db.Products.FindAsync(1);
            Assert.Equal(0, p!.Status);
        }

        [Fact]
        public async Task RestoreProductAsync_Sets_Status_1()
        {
            var db = BuildContext(nameof(RestoreProductAsync_Sets_Status_1));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var ok = await svc.RestoreProductAsync(2);

            Assert.True(ok);
            var p = await db.Products.FindAsync(2);
            Assert.Equal(1, p!.Status);
        }

        [Fact]
        public async Task BulkUpdateStatusAsync_Updates_Multiple_Products()
        {
            var db = BuildContext(nameof(BulkUpdateStatusAsync_Updates_Multiple_Products));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var req = new BulkUpdateProductStatusDto
            {
                ProductIds = new List<int> { 1, 2 },
                Status = 0
            };

            var (updatedCount, updatedProducts) = await svc.BulkUpdateStatusAsync(req, updatedBy: "tester");

            Assert.Equal(2, updatedCount);
            var p1 = await db.Products.FindAsync(1);
            var p2 = await db.Products.FindAsync(2);
            Assert.Equal(0, p1!.Status);
            Assert.Equal(0, p2!.Status);
            Assert.Equal(2, updatedProducts.Count());
        }

        [Fact]
        public async Task BulkUpdateStatusAsync_Throws_When_Ids_Empty()
        {
            var db = BuildContext(nameof(BulkUpdateStatusAsync_Throws_When_Ids_Empty));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var req = new BulkUpdateProductStatusDto
            {
                ProductIds = new List<int>(),
                Status = 1
            };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.BulkUpdateStatusAsync(req, "tester"));
        }

        [Fact]
        public async Task BulkUpdateStatusAsync_Throws_When_NotFound()
        {
            var db = BuildContext(nameof(BulkUpdateStatusAsync_Throws_When_NotFound));
            await SeedBasicAsync(db);
            var mapper = BuildMapper();
            var svc = new ProductService(db, mapper.Object);

            var req = new BulkUpdateProductStatusDto
            {
                ProductIds = new List<int> { 999 },
                Status = 1
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.BulkUpdateStatusAsync(req, "tester"));
        }
    }
}
