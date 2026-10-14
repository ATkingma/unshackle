using AutoMapper;

namespace Shop;

public class Series
{
    public int Id { get; set; }
}

public class SeriesDto
{
    public int Id { get; set; }
}

// Kavita: CreateMap<Series, SeriesDto> in AutoMapperProfiles and again in AutoMapperSeriesProfile.
public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Series, SeriesDto>();
    }
}

public class AutoMapperSeriesProfile : Profile
{
    public AutoMapperSeriesProfile()
    {
        CreateMap<Series, SeriesDto>();
    }
}

public class SeriesService
{
    public SeriesDto Get(IMapper mapper, Series series) => mapper.Map<SeriesDto>(series);
}
