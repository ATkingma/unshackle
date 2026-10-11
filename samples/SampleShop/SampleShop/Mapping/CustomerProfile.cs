using AutoMapper;
using SampleShop.Domain;
using SampleShop.Dtos;

namespace SampleShop.Mapping;

// Picked up by the assembly scan in AddSampleShop, not by an explicit type.
public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Money, string>().ConvertUsing<MoneyToStringConverter>();

        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName, o => o.MapFrom<FullNameResolver>())
            .ForMember(d => d.TagList, o => o.MapFrom(s => string.Join(",", s.Tags)))
            .ForMember(d => d.Since, o => o.ConvertUsing(new DateToStringConverter(), s => s.CreatedAt))
            .ForMember(d => d.IsMapped, o => o.Ignore())
            .AfterMap((_, d) => d.IsMapped = true);

        // Nested type: mapped by its own map, both ways.
        CreateMap<Address, AddressDto>().ReverseMap();

        CreateMap<Product, ProductDto>();
    }
}

public class FullNameResolver : IValueResolver<Customer, CustomerDto, string>
{
    public string Resolve(Customer source, CustomerDto destination, string destMember, ResolutionContext context) =>
        $"{source.FirstName} {source.LastName}";
}

public class MoneyToStringConverter : ITypeConverter<Money, string>
{
    public string Convert(Money source, string destination, ResolutionContext context) =>
        $"{source.Amount:0.00} {source.Currency}";
}

public class DateToStringConverter : IValueConverter<DateTime, string>
{
    public string Convert(DateTime sourceMember, ResolutionContext context) => sourceMember.ToString("yyyy-MM-dd");
}
