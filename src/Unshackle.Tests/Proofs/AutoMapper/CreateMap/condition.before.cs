using System;
using AutoMapper;

namespace Shop;

public class Activity
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
}

public class ActivityDto
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Activity, ActivityDto>()
            .ForMember(dest => dest.DocumentId, opt => opt.Condition(src => src.DocumentId != Guid.Empty));
    }
}
