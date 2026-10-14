using AutoMapper;

namespace Shop;

public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
}

public class ProductDto
{
    public int Id { get; set; }

    public string GetCode() => "P" + Id;
}

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>().ReverseMap();
    }
}
