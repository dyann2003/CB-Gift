using AutoMapper;
using CB_Gift.DTOs;
using CB_Gift.Models;

namespace CB_Gift.Mapper
{
    public class ProductProfile : Profile
    {
        public ProductProfile() 
        {
            // Map từ Entity → DTO
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.CategoryName))
                .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.ProductVariants));

            CreateMap<ProductVariant, ProductVariantDto>();

            // Map từ DTO → Entity
            CreateMap<ProductCreateDto, Product>();
            CreateMap<ProductUpdateDto, Product>();
            CreateMap<ProductVariantCreateDto, ProductVariant>();
        }
    }
}
