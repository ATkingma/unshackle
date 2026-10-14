using System;
using AutoMapper;

namespace Shop;

public class Activity
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
}

public class ActivityDto
{
    public int Id { get; set; }
    public string Heading { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Activity, ActivityDto>()
            .ForPath(dest => dest.Heading, opt => opt.MapFrom(src => src.Title));
    }
}
