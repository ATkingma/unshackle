using System;
using AutoMapper;

namespace Shop;

public class Order
{
    public string? Note { get; set; }
}

public class OrderDto
{
    public string Note { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Note, o => o.NullSubstitute("-"));
    }
}
