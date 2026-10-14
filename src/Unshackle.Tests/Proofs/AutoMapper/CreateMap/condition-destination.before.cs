using System;
using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public int Version { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public int Version { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Version, o => o.Condition((s, d) => s.Version > d.Version));
    }
}
