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
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.Secret, o => o.Ignore())
            .ForMember(dest => dest.Label, opt => opt.Ignore());
        CreateMap<Tag, TagDto>().ForMember(d => d.Rank, o => o.Ignore());
    }
}
