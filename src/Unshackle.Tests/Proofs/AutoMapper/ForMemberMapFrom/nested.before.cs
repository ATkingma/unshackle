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
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Shipping, o => o.MapFrom(s => s.Home))
            .ForMember(d => d.Previous, o => o.MapFrom(s => s.History));
        CreateMap<Address, AddressDto>();
    }
}
