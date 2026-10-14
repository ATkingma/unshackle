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
    }
}

public static class OrderProfileExtensions
{
    public static ActivityDto ToActivityDto(this Activity source) => new()
    {
        Id = source.Id,
        Heading = source.Title,
    };
}
