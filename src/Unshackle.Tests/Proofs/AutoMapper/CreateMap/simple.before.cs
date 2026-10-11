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
        CreateMap<Product, ProductDto>();
        CreateMap<Tag, TagDto>();
    }
}
