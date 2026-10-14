#nullable enable
using System.Collections.Generic;
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
        CreateMap<Order, OrderDto>();
        CreateMap<Address, AddressDto>();
    }
}
