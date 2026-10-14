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
    }
}

public static class OrderProfileExtensions
{
    public static ActivityDto ToActivityDto(this Activity source) => new()
    {
        Id = source.Id,
        DocumentId = source.DocumentId != Guid.Empty ? source.DocumentId : default,
    };
}
