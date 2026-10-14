using AutoMapper;

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
            CreateMap<BookmarkPair, BookmarkDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Bookmark.Id))
                .ForMember(dest => dest.Page, opt => opt.MapFrom(src => src.Bookmark.Page))
                .ForMember(dest => dest.Series, opt => opt.MapFrom(src => src.Series));
            CreateMap<Series, SeriesDto>();
        }
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

        public Mapping.BookmarkDto Get(Mapping.BookmarkPair pair) => _mapper.Map<Mapping.BookmarkDto>(pair);

        public Mapping.SeriesDto GetSeries(Mapping.Series series) => _mapper.Map<Mapping.SeriesDto>(series);
    }
}
