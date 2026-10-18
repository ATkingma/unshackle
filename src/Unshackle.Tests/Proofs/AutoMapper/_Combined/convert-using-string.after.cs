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
    }
}

public static class ProductProfileExtensions
{
    public static string ToMappedString(this Money source) => new MoneyToStringConverter().Convert(source, null, null);

    public static ProductDto ToProductDto(this Product source) => new()
    {
        Name = source.Name,
        Price = source.Price.ToMappedString(),
    };
}

public class ProductService
{
    private readonly IMapper _mapper;

    public ProductService(IMapper mapper) => _mapper = mapper;

    public string Price(Money money) => money.ToMappedString();

    public ProductDto Get(Product product) => product.ToProductDto();
}
