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

                // Xoá variants trước (nếu có)
                if (product.ProductVariants.Any())
                    _context.ProductVariants.RemoveRange(product.ProductVariants);

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return true;
            }
        }
}
