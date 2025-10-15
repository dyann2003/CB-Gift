using AutoMapper;
using CB_Gift.DTOs;
using CB_Gift.Models;

namespace CB_Gift.Mapper
{
    public class MapperProfile : Profile
    {
       public MapperProfile() 
        {
            CreateMap<Category, CategoryDto>();

            CreateMap<Tag, TagDto>();

            // DTO -> Model
            CreateMap<CreateTagDto, Tag>();
            CreateMap<UpdateTagDto, Tag>();

            CreateMap<UpdateCategoryDto, Category>();
            CreateMap<CreateCategoryDto, Category>();

            // === CHỨC NĂNG CỦA MANAGER ===

            // 1. Ánh xạ từ DTO dùng để gán Designer cho Seller sang Model
            // Dùng khi Manager tạo một mối quan hệ mới.
            CreateMap<AssignDesignerDto, DesignerSeller>();

            // 2. Ánh xạ từ Model DesignerSeller sang DTO để hiển thị
            // Dùng khi Manager cần xem danh sách các mối quan hệ đã tạo.
            // Các trường DesignerName và SellerName sẽ được xử lý ở Service Layer.
            CreateMap<DesignerSeller, DesignerSellerDto>()
                 .ForMember(
                    dest => dest.DesignerName,
                    opt => opt.MapFrom(src => src.DesignerUser.FullName) // Lấy OrderCode từ bảng Order liên quan
                )
                .ForMember(
                    dest => dest.SellerName,
                    opt => opt.MapFrom(src => src.SellerUser.FullName) 
                );
            CreateMap<ProductDetails, ProductVariant>();
            // === CHỨC NĂNG CỦA DESIGNER ===

            // 3. Ánh xạ từ Model OrderDetail sang DTO hiển thị Task cho Designer
            // Đây là mapping phức tạp hơn vì nó "làm phẳng" dữ liệu từ nhiều bảng.
            CreateMap<OrderDetail, DesignTaskDto>()
                .ForMember(
                    dest => dest.OrderCode,
                    opt => opt.MapFrom(src => src.Order.OrderCode) 
                )
                .ForMember(
                    dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.ProductVariant.Product.ProductName) 
                )
                 .ForMember(
                    dest => dest.ProductDescribe,
                    opt => opt.MapFrom(src => src.ProductVariant.Product.Describe)
                )
                 .ForMember(
                    dest => dest.ProductTemplate,
                    opt => opt.MapFrom(src => src.ProductVariant.Product.Template)
                )
                .ForMember(
                    dest => dest.ProductDetails,
                    opt => opt.MapFrom(src => src.ProductVariant) // Ánh xạ toàn bộ đối tượng ProductVariant đã Include
                ); 

            // Các DTO khác không cần mapping trực tiếp:
            // - UploadDesignDto: Dùng để nhận IFormFile từ request, được xử lý thủ công trong service.
            // - AssignDesignerToOrderDetailDto: Chỉ chứa thông tin để service tìm và cập nhật OrderDetail, không phải map toàn bộ đối tượng.

        }

    }
}
