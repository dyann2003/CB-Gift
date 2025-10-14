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
    }
   
}

