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
        public Mapping() =>
            CreateMap<Activity, ActivitySummaryDto>(MemberList.None)
                .ForMember(dest => dest.Notes, options => options.Ignore());
    }
}
