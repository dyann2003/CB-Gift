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

                CreateMap<Order, OrderWithDetailsDto>()
                    .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.EndCustomer.Name))
                    .ForMember(dest => dest.StatusOderName, opt => opt.MapFrom(src => src.StatusOrderNavigation.NameVi))
                    .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.OrderDetails));

                CreateMap<OrderDetail, OrderDetailDto>()
                    .ForMember(dest => dest.OrderDetailID, opt => opt.MapFrom(src => src.OrderDetailId))
                    .ForMember(dest => dest.ProductVariantID, opt => opt.MapFrom(src => src.ProductVariantId))
                    .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.ProductVariant.TotalCost))
                    .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                    .ForMember(dest => dest.LinkImg, opt => opt.MapFrom(src => src.LinkImg))
                    .ForMember(dest => dest.NeedDesign, opt => opt.MapFrom(src => src.NeedDesign));

            // DTO → Entity (dùng khi tạo)
            CreateMap<OrderCreateRequest, Order>()
                .ForMember(dest => dest.EndCustomerId, opt => opt.MapFrom(src => src.EndCustomerID))
                .ForMember(dest => dest.TotalCost, opt => opt.MapFrom(src => src.TotalCost))
                .ForMember(dest => dest.CostScan, opt => opt.MapFrom(src => src.CostScan ?? 1))
                .ForMember(dest => dest.ActiveTts, opt => opt.MapFrom(src => src.ActiveTTS))
                .ForMember(dest => dest.ProductionStatus, opt => opt.MapFrom(src => src.ProductionStatus ?? "CREATED"))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus ?? "UNPAID"))
                .ForMember(dest => dest.StatusOrder, opt => opt.MapFrom(_ => 1))
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
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price ?? 0))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(_ => DateTime.UtcNow));
        }
        }

    }

