using System;
using AutoMapper;
using CB_Gift.DTOs;
using CB_Gift.Models;

namespace CB_Gift.Mapper
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            // ===== Category / Tag =====
            CreateMap<Category, CategoryDto>();

            CreateMap<Tag, TagDto>();

            // DTO -> Entity: ignore các field không có trong DTO (id, navigation)
            CreateMap<CreateTagDto, Tag>()
                .ForMember(d => d.TagsId, opt => opt.Ignore())
                .ForMember(d => d.Products, opt => opt.Ignore());

            CreateMap<UpdateTagDto, Tag>()
                .ForMember(d => d.TagsId, opt => opt.Ignore())
                .ForMember(d => d.Products, opt => opt.Ignore());

            CreateMap<UpdateCategoryDto, Category>()
                .ForMember(d => d.CategoryId, opt => opt.Ignore())
                .ForMember(d => d.Products, opt => opt.Ignore());

            CreateMap<CreateCategoryDto, Category>()
                .ForMember(d => d.CategoryId, opt => opt.Ignore())
                .ForMember(d => d.Products, opt => opt.Ignore());

            // ===== Manager =====
            CreateMap<AssignDesignerDto, DesignerSeller>()
                .ForMember(d => d.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(d => d.CreatedByUserId, opt => opt.Ignore())
                .ForMember(d => d.DesignerUser, opt => opt.Ignore())
                .ForMember(d => d.SellerUser, opt => opt.Ignore());

            CreateMap<DesignerSeller, DesignerSellerDto>()
                .ForMember(d => d.DesignerName, opt => opt.MapFrom(s => s.DesignerUser != null ? s.DesignerUser.FullName : null))
                .ForMember(d => d.SellerName, opt => opt.MapFrom(s => s.SellerUser != null ? s.SellerUser.FullName : null));

            // ===== Designer =====
            // Map từ ProductVariant -> ProductDetails (để gán vào DesignTaskDto.ProductDetails)
            CreateMap<ProductVariant, ProductDetails>()
                .ForMember(d => d.ProductVariantId, opt => opt.MapFrom(s => s.ProductVariantId.ToString()))
                .ForMember(d => d.LengthCm, opt => opt.MapFrom(s => s.LengthCm))
                .ForMember(d => d.HeightCm, opt => opt.MapFrom(s => s.HeightCm))
                .ForMember(d => d.WidthCm, opt => opt.MapFrom(s => s.WidthCm))
                .ForMember(d => d.ThicknessMm, opt => opt.MapFrom(s => s.ThicknessMm))
                .ForMember(d => d.SizeInch, opt => opt.MapFrom(s => s.SizeInch))
                .ForMember(d => d.Layer, opt => opt.MapFrom(s => s.Layer))
                .ForMember(d => d.CustomShape, opt => opt.MapFrom(s => s.CustomShape))
                .ForMember(d => d.Sku, opt => opt.MapFrom(s => s.Sku));

            // OrderDetail -> DesignTaskDto
            CreateMap<OrderDetail, DesignTaskDto>()
                .ForMember(d => d.OrderId, opt => opt.MapFrom(s => s.OrderId))
                .ForMember(d => d.OrderCode, opt => opt.MapFrom(s => s.Order.OrderCode))
                .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.ProductVariant.Product.ProductName))
                .ForMember(d => d.ProductDescribe, opt => opt.MapFrom(s => s.ProductVariant.Product.Describe))
                .ForMember(d => d.ProductTemplate, opt => opt.MapFrom(s => s.ProductVariant.Product.Template))
                .ForMember(d => d.ProductionStatus, opt => opt.MapFrom(s => s.ProductionStatus.HasValue ? s.ProductionStatus.Value.ToString() : null))
                .ForMember(d => d.LinkThankCard, opt => opt.MapFrom(s => s.LinkThanksCard)) // khác tên: LinkThanksCard -> LinkThankCard
                .ForMember(d => d.OrderStatus, opt => opt.MapFrom(s => s.Order.StatusOrder))
                .ForMember(d => d.ProductDetails, opt => opt.MapFrom(s => s.ProductVariant))
                .ForMember(dest => dest.Reason, opt => opt.Ignore());
        }
    }
}
