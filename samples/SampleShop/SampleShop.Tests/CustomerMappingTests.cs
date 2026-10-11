using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using SampleShop.Domain;
using SampleShop.Dtos;

namespace SampleShop.Tests;

public class CustomerMappingTests
{
    private readonly IMapper _mapper = new ServiceCollection().AddSampleShop().BuildServiceProvider().GetRequiredService<IMapper>();

    private static Customer Ada() => new()
    {
        Id = 7,
        FirstName = "Ada",
        LastName = "Lovelace",
        Address = new Address { Street = "St James's Square", City = "London", Country = "UK" },
        Tags = ["vip", "early"],
        CreatedAt = new DateTime(2026, 3, 4),
    };

    [Fact]
    public void Customer_UsesResolverConverterAndAfterMap()
    {
        var dto = _mapper.Map<CustomerDto>(Ada());

        dto.FullName.Should().Be("Ada Lovelace");
        dto.TagList.Should().Be("vip,early");
        dto.Since.Should().Be("2026-03-04");
        dto.IsMapped.Should().BeTrue();
        dto.Address.City.Should().Be("London");
    }

    [Fact]
    public void Collection_MapsEveryItem()
    {
        var dtos = _mapper.Map<List<CustomerDto>>(new List<Customer> { Ada(), Ada() });

        dtos.Should().HaveCount(2);
        dtos.Should().OnlyContain(d => d.FullName == "Ada Lovelace");
    }

    [Fact]
    public void Address_ReverseMap()
    {
        var address = _mapper.Map<Address>(new AddressDto { City = "Paris" });

        address.City.Should().Be("Paris");
    }

    [Fact]
    public void Product_UsesTypeConverterForMoney()
    {
        var dto = _mapper.Map<ProductDto>(new Product("B-1", "Book", new Money(10m, "EUR")));

        dto.Price.Should().Be("10.00 EUR");
    }
}
