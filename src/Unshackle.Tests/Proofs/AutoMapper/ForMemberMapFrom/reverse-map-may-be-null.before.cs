using System;
using AutoMapper;

namespace Shop;

public class User
{
    public Guid Id { get; set; }
}

public class OrganizationUser
{
    public Guid Id { get; set; }
    public User User { get; set; } = new();
}

public class UserProjectAccessPolicy
{
    public Guid Id { get; set; }
    public Guid OrganizationUserId { get; set; }
}

public class UserProjectAccessPolicyEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationUserId { get; set; }
    public OrganizationUser OrganizationUser { get; set; } = new();
}

public class AccessPolicyMapperProfile : Profile
{
    public AccessPolicyMapperProfile()
    {
        CreateMap<UserProjectAccessPolicy, UserProjectAccessPolicyEntity>()
            .ForMember(dst => dst.OrganizationUser, opt => opt.Ignore())
            .ReverseMap()
            .ForMember(dst => dst.OrganizationUserId, opt => opt.MapFrom(src => src.OrganizationUser.Id));
    }
}
