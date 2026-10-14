using System;
using AutoMapper;

namespace Cfo.Cats.Application.Features.Activities.DTOs;

public class Activity
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
}

public class ActivitySummaryDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public string Notes { get; set; } = "";

    public class Mapping : Profile
    {
        public Mapping()
        {
        }
    }
}

public static class ActivitySummaryDtoMappingExtensions
{
    public static ActivitySummaryDto ToActivitySummaryDto(this Activity source) => new()
    {
        Id = source.Id,
        Description = source.Description,
    };
}
