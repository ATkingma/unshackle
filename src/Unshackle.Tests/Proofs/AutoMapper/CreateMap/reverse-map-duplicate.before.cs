using AutoMapper;

namespace Shop;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>().ReverseMap();
        CreateMap<ProductDto, Product>();
    }
}
