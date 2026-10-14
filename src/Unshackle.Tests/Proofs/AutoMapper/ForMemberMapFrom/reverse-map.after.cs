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
    }
}

public static class ArticleProfileExtensions
{
    public static ArticleDto ToArticleDto(this Article source) => new()
    {
        Id = source.Id,
        Title = source.Name,
    };

    public static Article ToArticle(this ArticleDto source) => new()
    {
        Id = source.Id,
        Name = source.Title,
        Note = source.Label,
    };
}
