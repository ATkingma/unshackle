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
            .ReverseMap()
            .ForPath(activity => activity.Title, opt => opt.MapFrom(dto => dto.Heading));
    }
}
