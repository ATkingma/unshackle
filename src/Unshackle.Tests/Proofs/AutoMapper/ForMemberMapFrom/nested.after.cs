using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Shop;

public class Address
{
    public string City { get; set; } = "";
}

public class AddressDto
{
    public string City { get; set; } = "";
}

public class Order
{
    public int Id { get; set; }
    public Address Home { get; set; } = new();
    public List<Address> History { get; set; } = [];
}

public class OrderDto
{
    public int Id { get; set; }
    public AddressDto Shipping { get; set; } = new();
    public AddressDto[] Previous { get; set; } = [];
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new()
    {
        Id = source.Id,
        Shipping = source.Home?.ToAddressDto(),
        Previous = source.History?.Select(x => x?.ToAddressDto()).ToArray() ?? [],
    };

    public static AddressDto ToAddressDto(this Address source) => new()
    {
        City = source.City,
    };
}
