using System.Collections.Generic;
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
        CreateMap<Order, OrderDto>();
        CreateMap<Address, Address>();
    }
}
