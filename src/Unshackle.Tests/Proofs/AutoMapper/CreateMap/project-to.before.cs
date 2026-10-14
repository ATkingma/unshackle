#nullable enable
using System.Collections.Generic;
using System.Linq;
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
        CreateMap<Series, SeriesDto>();
        CreateMap<Genre, GenreTagDto>();
        CreateMap<Person, PersonDto>();
    }
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
