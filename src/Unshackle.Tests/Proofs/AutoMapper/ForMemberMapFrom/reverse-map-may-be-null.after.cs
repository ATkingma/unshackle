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
    }
}

public static class AccessPolicyMapperProfileExtensions
{
    public static UserProjectAccessPolicyEntity ToUserProjectAccessPolicyEntity(this UserProjectAccessPolicy source) => new()
    {
        Id = source.Id,
        OrganizationUserId = source.OrganizationUserId,
    };

    public static UserProjectAccessPolicy ToUserProjectAccessPolicy(this UserProjectAccessPolicyEntity source) => new()
    {
        Id = source.Id,
        OrganizationUserId = source.OrganizationUser?.Id ?? default,
    };
}
