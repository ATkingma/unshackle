using AutoMapper;

namespace Shop;

public class Product
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Secret { get; set; } = "";
}

public class ProductDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Secret { get; set; } = "";
    public string Label { get; set; } = "";
}

public class Tag
{
    public string Value { get; set; } = "";
}

public class TagDto
{
    public string Value { get; set; } = "";
    public int Rank { get; set; }
}

public class ProductProfile : Profile
{
    public ProductProfile()
    {
    }
}

public static class ProductProfileExtensions
{
    public static ProductDto ToProductDto(this Product source) => new()
    {
        Id = source.Id,
        Name = source.Name,
    };

    public static TagDto ToTagDto(this Tag source) => new()
    {
        Value = source.Value,
    };
}
