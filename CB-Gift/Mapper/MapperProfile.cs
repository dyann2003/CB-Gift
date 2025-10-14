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

        }

    }
}
