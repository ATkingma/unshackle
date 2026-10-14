using System;
using AutoMapper;

namespace Shop;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class GroupProjectAccessPolicy
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public bool Read { get; set; }
    public string Note { get; set; } = "";
}

public class GroupProjectAccessPolicyEntity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public bool Read { get; set; }
    public string Note { get; set; } = "";
    public Project GrantedProject { get; set; } = new();
    public Project Group { get; set; } = new();
}

public class AccessPolicyMapperProfile : Profile
{
    public AccessPolicyMapperProfile()
    {
    }
}

public static class AccessPolicyMapperProfileExtensions
{
    public static GroupProjectAccessPolicyEntity ToGroupProjectAccessPolicyEntity(this GroupProjectAccessPolicy source) => new()
    {
        Id = source.Id,
        GroupId = source.GroupId,
        Read = source.Read,
    };

    public static GroupProjectAccessPolicy ToGroupProjectAccessPolicy(this GroupProjectAccessPolicyEntity source) => new()
    {
        Id = source.Id,
        GroupId = source.GroupId,
        Note = source.Note,
    };
}
