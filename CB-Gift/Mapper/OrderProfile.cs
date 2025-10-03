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
            }
        }

    }

