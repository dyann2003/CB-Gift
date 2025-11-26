using AutoMapper;
using CB_Gift.DTOs;
using CB_Gift.Models;

namespace CB_Gift.Mapper
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.EndCustomerId))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.EndCustomer.Name))
                .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.SellerUserId))
                .ForMember(dest => dest.StatusOderName, opt => opt.MapFrom(src => src.StatusOrderNavigation.NameVi));
            CreateMap<OrderDto, Order>();
            CreateMap<Order, OrderWithDetailsDto>()
           .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.EndCustomer.CustId))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.EndCustomer.Name))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.EndCustomer.Phone))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.EndCustomer.Email))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.EndCustomer.Address))
            .ForMember(dest => dest.Address1, opt => opt.MapFrom(src => src.EndCustomer.Address1))
            .ForMember(dest => dest.Zipcode, opt => opt.MapFrom(src => src.EndCustomer.Zipcode))
            .ForMember(dest => dest.ShipState, opt => opt.MapFrom(src => src.EndCustomer.ShipState))
            .ForMember(dest => dest.ShipCity, opt => opt.MapFrom(src => src.EndCustomer.ShipCity))
            .ForMember(dest => dest.ShipCountry, opt => opt.MapFrom(src => src.EndCustomer.ShipCountry))
            .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.SellerUserId))
            .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.SellerUser.FullName))
            .ForMember(dest => dest.StatusOderName, opt => opt.MapFrom(src => src.StatusOrderNavigation.NameVi))
            .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.OrderDetails));
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.SellerUser.FullName))
                .ForMember(dest => dest.StatusOderName, opt => opt.MapFrom(src => src.StatusOrderNavigation.Code))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.OrderDetails));

            CreateMap<OrderDetail, OrderDetailDto>()
                    .ForMember(dest => dest.OrderDetailID, opt => opt.MapFrom(src => src.OrderDetailId))
                    .ForMember(dest => dest.ProductVariantID, opt => opt.MapFrom(src => src.ProductVariantId))
                    .ForMember(dest => dest.Sku,opt => opt.MapFrom(src=>src.ProductVariant.Sku))
                    .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.ProductVariant.SizeInch))
                    .ForMember(dest => dest.Layer, opt => opt.MapFrom(src => src.ProductVariant.Layer))
                    .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductVariant.Product.ProductName))
                    //.ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.ProductVariant.TotalCost))
                    .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                    .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                     .ForMember(dest => dest.AssignedAt, opt => opt.MapFrom(src => src.AssignedAt))
                     .ForMember(dest => dest.AssignedDesignerUserId, opt => opt.MapFrom(src => src.AssignedDesignerUserId))
                    .ForMember(dest => dest.LinkImg, opt => opt.MapFrom(src => src.LinkImg))
                    .ForMember(dest => dest.NeedDesign, opt => opt.MapFrom(src => src.NeedDesign))
                    .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.ProductionStatus));

            // DTO → Entity (dùng khi tạo)
            CreateMap<OrderCreateRequest, Order>()
                .ForMember(dest => dest.EndCustomerId, opt => opt.MapFrom(src => src.EndCustomerID))
                .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => src.TotalCost))
                .ForMember(dest => dest.CostScan, opt => opt.MapFrom(src => src.CostScan ?? 1))
                .ForMember(dest => dest.ActiveTts, opt => opt.MapFrom(src => src.ActiveTTS))
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.OrderCode))
                .ForMember(dest => dest.ProductionStatus, opt => opt.MapFrom(src => src.ProductionStatus ?? "Created"))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus ?? "UNPAID"))
                .ForMember(dest => dest.StatusOrder, opt => opt.MapFrom(src => src.StatusOrder ?? 1))
                .ForMember(dest => dest.ToDistrictId, opt => opt.MapFrom(src => src.ToDistrictId))
                .ForMember(dest => dest.ToProvinceId, opt => opt.MapFrom(src => src.ToProvinceId))
                .ForMember(dest => dest.ToWardCode, opt => opt.MapFrom(src => src.ToWardCode))
                .ForMember(dest => dest.CreationDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

            CreateMap<OrderDetailCreateRequest, OrderDetail>()
                .ForMember(dest => dest.ProductVariantId, opt => opt.MapFrom(src => src.ProductVariantID))
                .ForMember(dest => dest.LinkImg, opt => opt.MapFrom(src => src.LinkImg))
                .ForMember(dest => dest.LinkThanksCard, opt => opt.MapFrom(src => src.LinkThanksCard))
                .ForMember(dest => dest.LinkFileDesign, opt => opt.MapFrom(src => src.LinkDesign))
                .ForMember(dest => dest.Accessory, opt => opt.MapFrom(src => src.Accessory))
                .ForMember(dest => dest.Note, opt => opt.MapFrom(src => src.Note))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.NeedDesign, opt => opt.MapFrom(src => src.NeedDesign))
                //.ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price ?? 0))
                .ForMember(dest => dest.Price, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));
            // test MakeOrder
            CreateMap<EndCustomerCreateRequest, EndCustomer>();
            // UpdateOrder
            CreateMap<OrderCoreUpdateRequest, Order>()
                // Bỏ qua các thuộc tính không có trong DTO update để tránh ghi đè null
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<OrderDetailUpdateRequest, OrderDetail>()
                // Khi update, không được set lại CreatedDate
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                // Cũng không map OrderDetailID vì đây là khóa chính
                .ForMember(dest => dest.OrderDetailId, opt => opt.Ignore())
                .ForMember(dest => dest.ProductVariantId, opt => opt.MapFrom(src => src.ProductVariantID));

            CreateMap<OrderDetail, MakeOrderDetailResponse>()
                .ForMember(dest => dest.ProductVariantID, opt => opt.MapFrom(src => src.ProductVariantId));

            // THÊM CẤU HÌNH BỊ THIẾU: Order -> MakeOrderResponse
            CreateMap<Order, MakeOrderResponse>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.EndCustomer.Name))
                .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.OrderDetails));
            CreateMap<EndCustomerUpdateRequest, EndCustomer>();
        }
    }

}

