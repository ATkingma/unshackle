using AutoMapper;

namespace Shop;

public class Product
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; init; }
    public int Stock;
    public string Secret { get; set; } = "";
}

public class ProductDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; init; }
    public int Stock;
    public string Label => Name + " (" + Id + ")";
}

public class Tag
{
    public string Value { get; set; } = "";
}

public struct TagDto
{
    public string Value { get; set; }
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
        Price = source.Price,
        Stock = source.Stock,
    };

    public static TagDto ToTagDto(this Tag source) => new()
    {
        Value = source.Value,
    };
}
