using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Shop;

public class Address
{
    public string City { get; set; } = "";
}

public class Order
{
    public Address Address { get; set; } = new();
    public List<Address> History { get; set; } = [];
}

public class OrderDto
{
    public Address Address { get; set; } = new();
    public List<Address> History { get; set; } = [];
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
        Address = source.Address?.ToAddress(),
        History = source.History?.Select(x => x?.ToAddress()).ToList() ?? [],
    };

    public static Address ToAddress(this Address source) => new()
    {
        City = source.City,
    };
}
