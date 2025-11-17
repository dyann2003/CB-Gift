using AutoMapper;
using CB_Gift.Data;
using CB_Gift.DTOs;
using CB_Gift.Models;
using CB_Gift.Services.IService;
using Microsoft.EntityFrameworkCore;

namespace CB_Gift.Services
{
    public class ProductService : IProductService
    {
        private readonly CBGiftDbContext _context;
        private readonly IMapper _mapper;

        public ProductService(CBGiftDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // 🔹 Get all products
        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        //Get All Product Have Status = 1 // các sản phẩm active
        public async Task<IEnumerable<ProductDto>> GetAllProductsHaveStatusTrueAsync()
        {
            var products = await _context.Products
                .Include(p => p.ProductVariants)
                .Where(p => p.Status == 1) // chỉ lấy sản phẩm hiển thị
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        //Get All Product Have Status = 0 -- các sản phẩm bị ẩn
        public async Task<IEnumerable<ProductDto>> GetHiddenProductsAsync()
        {
            var products = await _context.Products
                .Include(p => p.ProductVariants)
                .Where(p => p.Status == 0) // chỉ lấy sản phẩm bị ẩn
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        // 🔹 Get product by id
        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            return product == null ? null : _mapper.Map<ProductDto>(product);
        }

        // 🔹 Create new product (with variants)
        public async Task<ProductDto> CreateAsync(ProductCreateDto dto)
        {
            var product = _mapper.Map<Product>(dto);

            // ✅ Gán Tag nếu có
            if (dto.TagIds != null && dto.TagIds.Any())
            {
                // Lấy ra các Tag từ DB theo danh sách ID
                var tags = await _context.Tags
                    .Where(t => dto.TagIds.Contains(t.TagsId))
                    .ToListAsync();

                foreach (var tag in tags)
                {
                    product.Tags.Add(tag); // EF sẽ tự tạo bản ghi trong bảng trung gian ProductTag
                }
            }

            if (dto.Variants != null && dto.Variants.Any())
            {
                foreach (var variantDto in dto.Variants)
                {
                    var variant = _mapper.Map<ProductVariant>(variantDto);
                    product.ProductVariants.Add(variant);
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();


            // Map lại để lấy ID sau khi tạo
            return _mapper.Map<ProductDto>(product);
        }

        // 🔹 Update product
        /*public async Task<ProductDto?> UpdateAsync(int id, ProductUpdateDto dto)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return null;

            _mapper.Map(dto, product);

            // Nếu có variants gửi lên
            if (dto.Variants != null)
            {
                // Xoá toàn bộ variants cũ, rồi thêm lại mới (cách dễ & rõ ràng)
                _context.ProductVariants.RemoveRange(product.ProductVariants);
                product.ProductVariants.Clear();

                foreach (var v in dto.Variants)
                {
                    var variant = _mapper.Map<ProductVariant>(v);
                    product.ProductVariants.Add(variant);
                }
            }

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProductDto>(product);
        }*/
        // 🟢 UPDATE PRODUCT VÀ PRODUCTVARIANT
        public async Task<ProductDto> UpdateAsync(int id, ProductUpdateDto request)
        {
            var existingProduct = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (existingProduct == null)
                throw new Exception("Product not found");

            // Map thông tin Product chính
            _mapper.Map(request, existingProduct);

            // Danh sách Variant hiện có trong DB
            var existingVariants = existingProduct.ProductVariants.ToList();

            // Danh sách Variant gửi lên
            var incomingVariants = request.Variants ?? new List<ProductVariantUpdateDto>();

            // 🔹 1. Cập nhật và thêm mới
            foreach (var variantDto in incomingVariants)
            {
                if (variantDto.ProductVariantId == 0)
                {
                    // Thêm mới
                    var newVariant = _mapper.Map<ProductVariant>(variantDto);
                    newVariant.ProductId = id;
                    _context.ProductVariants.Add(newVariant);
                }
                else
                {
                    // Cập nhật nếu đã tồn tại
                    var existingVariant = existingVariants
                        .FirstOrDefault(v => v.ProductVariantId == variantDto.ProductVariantId);

                    if (existingVariant != null)
                    {
                        _mapper.Map(variantDto, existingVariant);
                    }
                }
            }

            // 🔹 2. Xóa những variant không còn trong request
            var variantIdsFromRequest = incomingVariants
                .Where(v => v.ProductVariantId != 0)
                .Select(v => v.ProductVariantId)
                .ToList();

            var variantsToRemove = existingVariants
                .Where(v => !variantIdsFromRequest.Contains(v.ProductVariantId))
                .ToList();

            _context.ProductVariants.RemoveRange(variantsToRemove);

            // 🔹 3. Lưu thay đổi
            await _context.SaveChangesAsync();

            // 🔹 4. Trả về DTO sau khi cập nhật
            var updatedProduct = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            return _mapper.Map<ProductDto>(updatedProduct);
        }


        // 🔹 Delete product
        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return false;

            // Không xóa variants vì sợ có trong OrderDetail, có rồi thì không được xóa
            //if (product.ProductVariants.Any())
            //    _context.ProductVariants.RemoveRange(product.ProductVariants);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return true;
        }
        // Ẩn Product đi.
        public async Task<bool> SoftDeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;
            product.Status = 0; // ẩn sản phẩm
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return true;
        }
        // Hiển thị Product lên.
        public async Task<bool> RestoreProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;
            product.Status = 1; // hiển thị lại sản phẩm
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return true;
        }
        //Update Status Product theo danh sách
        public async Task<(int updatedCount, IEnumerable<object> updatedProducts)> BulkUpdateStatusAsync(BulkUpdateProductStatusDto request, string updatedBy)
        {
            if (request.ProductIds == null || !request.ProductIds.Any())
                throw new ArgumentException("Danh sách sản phẩm không được để trống.");

            var products = await _context.Products
                .Where(p => request.ProductIds.Contains(p.ProductId))
                .ToListAsync();

            if (!products.Any())
                throw new KeyNotFoundException("Không tìm thấy sản phẩm nào khớp.");

            foreach (var product in products)
            {
                product.Status = request.Status;
            }

            await _context.SaveChangesAsync();

            var updated = products.Select(p => new
            {
                p.ProductId,
                p.ProductName,
                p.Status
            });

            return (products.Count, updated);
        }

        public async Task<(IEnumerable<ProductDto> products, int total)>
    GetFilteredAndPagedProductsAsync(
        string? searchTerm,
        int? status,
        string? sortColumn,
        string? sortDirection,
        int page,
        int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .Include(p => p.Tags)
                .AsNoTracking()
                .AsQueryable();

            // 🔍 Filter Status
            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            // 🔍 Search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();

                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(searchTerm) ||
                    (p.Category != null && p.Category.CategoryName.ToLower().Contains(searchTerm)) ||
                    p.Tags.Any(t => t.TagName.ToLower().Contains(searchTerm) ||
                                    t.TagCode.ToLower().Contains(searchTerm))
                );
            }

            // 🔍 Sort
            query = sortColumn?.ToLower() switch
            {
                "name" => sortDirection == "asc"
                    ? query.OrderBy(p => p.ProductName)
                    : query.OrderByDescending(p => p.ProductName),

                "price" => sortDirection == "asc"
                    ? query.OrderBy(p => p.ProductVariants.Min(v => (decimal?)v.TotalCost))
                    : query.OrderByDescending(p => p.ProductVariants.Min(v => (decimal?)v.TotalCost)),

                "category" => sortDirection == "asc"
                    ? query.OrderBy(p => p.Category.CategoryName)
                    : query.OrderByDescending(p => p.Category.CategoryName),

                _ => query.OrderByDescending(p => p.ProductId)
            };

            // 📌 Total for pagination
            var total = await query.CountAsync();

            // 📌 Pagination
            var result = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Mapping to ProductDto
            var mapped = _mapper.Map<IEnumerable<ProductDto>>(result);

            return (mapped, total);
        }

        public async Task<(int total, List<ProductDto> products)> FilterProductsAsync(
    string? searchTerm,
    string? category,
    int? status,
    int page,
    int pageSize)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(searchTerm) ||
                    (p.ProductCode != null && p.ProductCode.Contains(searchTerm)) ||
                    (p.Describe != null && p.Describe.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category.CategoryName == category);
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status);
            }

            var total = await query.CountAsync();

            var list = await query
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductCode = p.ProductCode,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.CategoryName,
                    Describe = p.Describe,
                    ItemLink = p.ItemLink,
                    Template = p.Template,
                    Status = p.Status,
                    Variants = p.ProductVariants.Select(v => new ProductVariantDto
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
                })
                .ToListAsync();

            return (total, list);
        }



    }

}

