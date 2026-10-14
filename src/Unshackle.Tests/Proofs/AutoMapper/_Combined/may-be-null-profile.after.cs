using AutoMapper;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Series
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class SeriesDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class Bookmark
    {
        public int Id { get; set; }
        public int Page { get; set; }
    }

    public class BookmarkPair
    {
        public Bookmark Bookmark { get; init; } = null!;
        public Series Series { get; init; } = null!;
    }

    public class BookmarkDto
    {
        public int Id { get; set; }
        public int Page { get; set; }
        public SeriesDto? Series { get; set; }
    }

    public class AppProfiles : Profile
    {
        public AppProfiles()
        {
        }
    }

    public static class AppProfilesExtensions
    {
        public static BookmarkDto ToBookmarkDto(this BookmarkPair source) => new()
        {
            Id = source.Bookmark?.Id ?? default,
            Page = source.Bookmark?.Page ?? default,
            Series = source.Series?.ToSeriesDto(),
        };

        public static SeriesDto ToSeriesDto(this Series source) => new()
        {
            Id = source.Id,
            Name = source.Name,
        };
    }
}

namespace Shop.Orders
{
    public class BookmarkService
    {
        private readonly IMapper _mapper;

        public BookmarkService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public Mapping.BookmarkDto Get(Mapping.BookmarkPair pair) => pair.ToBookmarkDto();

        public Mapping.SeriesDto GetSeries(Mapping.Series series) => series.ToSeriesDto();
    }
}
