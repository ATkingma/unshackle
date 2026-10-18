using System.Collections.Generic;
using AutoMapper;

namespace Shop;

// No map from DayEntity to int and no built-in conversion: AutoMapper throws for a non-empty list (measured, TAAK-271).
public class DayEntity
{
    public int Id { get; set; }
}

public class ScheduleEntity
{
    public List<DayEntity> Days { get; set; } = [];
}

public class ScheduleDto
{
    public List<int> Days { get; set; } = [];
}

public class ScheduleProfile : Profile
{
    public ScheduleProfile()
    {
        CreateMap<ScheduleEntity, ScheduleDto>();
    }
}
