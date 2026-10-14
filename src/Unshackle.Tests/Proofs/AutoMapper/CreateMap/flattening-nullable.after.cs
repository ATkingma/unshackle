#nullable enable
using AutoMapper;

namespace Reader;

public class Library
{
    public string Name { get; set; } = "";
}

public class Series
{
    public string Name { get; set; } = "";
    public int Pages { get; set; }
    public int Rating { get; set; }
}

public class Annotation
{
    public int Id { get; set; }
    public Library Library { get; set; } = null!;
    public Series? Series { get; set; }
}

public class AnnotationDto
{
    public int Id { get; set; }
    public string LibraryName { get; set; } = "";
    public string? SeriesName { get; set; }
    public int SeriesPages { get; set; }
    public int? SeriesRating { get; set; }
}

public class AnnotationProfile : Profile
{
    public AnnotationProfile()
    {
    }
}

public static class AnnotationProfileExtensions
{
    public static AnnotationDto ToAnnotationDto(this Annotation source) => new()
    {
        Id = source.Id,
        LibraryName = source.Library?.Name!,
        SeriesName = source.Series?.Name,
        SeriesPages = source.Series?.Pages ?? default,
        SeriesRating = source.Series?.Rating,
    };
}
