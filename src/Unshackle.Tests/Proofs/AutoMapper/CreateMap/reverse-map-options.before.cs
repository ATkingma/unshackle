using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Shop;

public class Role
{
    public string Name { get; set; } = "";
}

public class User
{
    public long Id { get; set; }
    public HashSet<Role> Roles { get; set; } = [];
}

public class UserDto
{
    public long Id { get; set; }
    public HashSet<Role> Roles { get; set; } = [];
}

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<User, UserDto>()
            .ReverseMap()
            .ForPath(user => user.Roles, opt => opt.MapFrom(userDto => userDto.Roles.Select(role => new Role { Name = role.Name }).ToHashSet()));
    }
}
