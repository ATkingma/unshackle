#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    }
}

public static class SeriesProfileExtensions
{
    public static SeriesDto ToSeriesDto(this Series source, int userId = 0) => new()
    {
        Id = source.Id,
        Name = source.Name,
        PagesRead = source.Readings.Where(r => r.AppUserId == userId).Sum(r => r.PagesRead),
    };

    public static Expression<Func<Series, SeriesDto>> SeriesToSeriesDtoProjection(int userId = 0) => source => new SeriesDto
    {
        Id = source.Id,
        Name = source.Name,
        PagesRead = source.Readings.Where(r => r.AppUserId == userId).Sum(r => r.PagesRead),
    };
}

public class SeriesQueries
{
    public IQueryable<SeriesDto> ForUser(IQueryable<Series> series, IMapper mapper, int userId) =>
        series.Select(SeriesProfileExtensions.SeriesToSeriesDtoProjection(userId));

    public IQueryable<SeriesDto> ForNobody(IQueryable<Series> series, IMapper mapper) =>
        series.Select(SeriesProfileExtensions.SeriesToSeriesDtoProjection());

    public IQueryable<SeriesDto> ForOwner(IQueryable<Series> series, IMapper mapper, Series owner) =>
        series.Select(SeriesProfileExtensions.SeriesToSeriesDtoProjection(owner.Id));

    public SeriesDto One(IMapper mapper, Series series) => series.ToSeriesDto();
}
