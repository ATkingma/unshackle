#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace Shop;

public class Genre
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
}

public class Person
{
    public string Name { get; set; } = "";
}

public class Series
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Person Writer { get; set; } = new();
    public Genre[] Genres { get; set; } = [];
    public List<int> Years { get; set; } = [];
}

public class GenreTagDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
}

public class PersonDto
{
    public string Name { get; set; } = "";
}

public class SeriesDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public PersonDto Writer { get; set; } = new();
    public GenreTagDto[] Genres { get; set; } = [];
    public List<int> Years { get; set; } = [];
}

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
    }
}

public static class AutoMapperProfilesExtensions
{
    public static SeriesDto ToSeriesDto(this Series source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Writer = source.Writer?.ToPersonDto()!,
        Genres = source.Genres?.Select(x => x?.ToGenreTagDto()!).ToArray() ?? [],
        Years = source.Years?.ToList() ?? [],
    };

    public static GenreTagDto ToGenreTagDto(this Genre source) => new()
    {
        Id = source.Id,
        Title = source.Title,
    };

    public static PersonDto ToPersonDto(this Person source) => new()
    {
        Name = source.Name,
    };

    public static Expression<Func<Series, SeriesDto>> SeriesToSeriesDtoProjection => source => new SeriesDto
    {
        Id = source.Id,
        Name = source.Name,
        Writer = source.Writer == null ? null! : new PersonDto { Name = source.Writer.Name },
        Genres = source.Genres.Select(x => new GenreTagDto { Id = x.Id, Title = x.Title }).ToArray(),
        Years = source.Years,
    };
}

public class SeriesRepository
{
    private readonly IMapper _mapper;

    public SeriesRepository(IMapper mapper)
    {
        _mapper = mapper;
    }

    public List<SeriesDto> All(IQueryable<Series> series) => series.ProjectTo<SeriesDto>(_mapper.ConfigurationProvider).ToList();
}
