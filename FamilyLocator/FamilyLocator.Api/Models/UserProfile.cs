using AutoMapper;
using FamilyLocator.DataLayer.DataBase.Entities;
using FamilyLocator.Models.Requests;
using FamilyLocator.Models.Responses;

namespace FamilyLocator.Api.Models
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<XIdentityUser, UserDto>()
                .ForMember(x=>x.Image,s=>s.MapFrom(o=>o.Image.Image));
            CreateMap<XIdentityUser, MoreUserDto>()
                .ForMember(x => x.Image, s => s.MapFrom(o => o.Image.Image)); 
            CreateMap<Family, FamilyDto>().ForMember(x=>x.Users,s=>s.MapFrom((o,_,_,c)=>o.Users.Select(c.Mapper.Map<MoreUserDto>)));
            CreateMap<XUCoordinates, CoordinatesDto>();
        }
    }
}
