using AutoMapper;

namespace Shop;

public class Article
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Note { get; set; } = "";
}

public class ArticleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Label { get; set; } = "";
}

public class ArticleProfile : Profile
{
    public ArticleProfile()
    {
        CreateMap<Article, ArticleDto>()
            .ForMember(d => d.Title, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Label, o => o.Ignore())
            .ReverseMap()
            .ForMember(d => d.Note, o => o.MapFrom(s => s.Label));
    }
}
