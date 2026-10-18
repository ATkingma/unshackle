using AutoMapper;

namespace Shop;

// CreateMap<Money, string>: a ToString extension would never be called (Money.ToString() wins), so the method is ToMappedString (TAAK-273).
public class Money
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
}

public class Product
{
    public string Name { get; set; } = "";
    public Money Price { get; set; } = new();
}

public class ProductDto
{
    public string Name { get; set; } = "";
    public string Price { get; set; } = "";
}

public class MoneyToStringConverter : ITypeConverter<Money, string>
{
    public string Convert(Money source, string destination, ResolutionContext context) => $"{source.Amount:0.00} {source.Currency}";
}

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Money, string>().ConvertUsing<MoneyToStringConverter>();
        CreateMap<Product, ProductDto>();
    }
}

public class ProductService
{
    private readonly IMapper _mapper;

    public ProductService(IMapper mapper) => _mapper = mapper;

    public string Price(Money money) => _mapper.Map<string>(money);

    public ProductDto Get(Product product) => _mapper.Map<ProductDto>(product);
}
