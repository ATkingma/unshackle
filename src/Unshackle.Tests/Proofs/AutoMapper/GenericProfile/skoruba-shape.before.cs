using AutoMapper;

namespace Skoruba.Admin.BusinessLogic.Identity.Mappers;

public class User
{
    public string UserName { get; set; } = "";
}

public class IdentityMapperProfile<TUserDto> : Profile
    where TUserDto : new()
{
    public IdentityMapperProfile()
    {
        CreateMap<User, TUserDto>();
    }
}
