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
        CreateMap<GroupProjectAccessPolicy, GroupProjectAccessPolicyEntity>()
            .ForMember(dst => dst.GrantedProject, opt => opt.Ignore())
            .ForMember(dst => dst.Group, opt => opt.Ignore())
            .ForMember(dst => dst.Note, opt => opt.Ignore())
            .ReverseMap()
            .ForMember(dst => dst.Read, opt => opt.Ignore());
    }
}
