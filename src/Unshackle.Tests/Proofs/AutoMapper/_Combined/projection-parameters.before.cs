#nullable enable
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace Shop;

public class Reading
{
    public int AppUserId { get; set; }
    public int PagesRead { get; set; }
}

public class Series
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Reading> Readings { get; set; } = [];
}

public class SeriesDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int PagesRead { get; set; }
}

// Kavita's AutoMapperSeriesProfile: the variable is a placeholder that ProjectTo fills per call.
public class SeriesProfile : Profile
{
    public SeriesProfile()
    {
        int userId = 0; // Placeholder, will be replaced at runtime

        CreateMap<Series, SeriesDto>()
            .ForMember(d => d.PagesRead, o => o.MapFrom(s => s.Readings.Where(r => r.AppUserId == userId).Sum(r => r.PagesRead)));
    }
}

public class SeriesQueries
{
    public IQueryable<SeriesDto> ForUser(IQueryable<Series> series, IMapper mapper, int userId) =>
        series.ProjectTo<SeriesDto>(mapper.ConfigurationProvider, new { userId });

    public IQueryable<SeriesDto> ForNobody(IQueryable<Series> series, IMapper mapper) =>
        series.ProjectTo<SeriesDto>(mapper.ConfigurationProvider);

    public IQueryable<SeriesDto> ForOwner(IQueryable<Series> series, IMapper mapper, Series owner) =>
        mapper.ProjectTo<SeriesDto>(series, new { userId = owner.Id });

    public SeriesDto One(IMapper mapper, Series series) => mapper.Map<SeriesDto>(series);
}
