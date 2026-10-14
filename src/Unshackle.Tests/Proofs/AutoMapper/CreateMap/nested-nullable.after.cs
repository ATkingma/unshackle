#nullable enable
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
    public Address Address { get; set; } = null!;
    public Address? Billing { get; set; }
    public List<Address> History { get; set; } = [];
}

public class OrderDto
{
    public AddressDto Address { get; set; } = null!;
    public AddressDto? Billing { get; set; }
    public List<AddressDto> History { get; set; } = [];
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
        Address = source.Address?.ToAddressDto()!,
        Billing = source.Billing?.ToAddressDto(),
        History = source.History?.Select(x => x?.ToAddressDto()!).ToList() ?? [],
    };

    public static AddressDto ToAddressDto(this Address source) => new()
    {
        City = source.City,
    };
}
