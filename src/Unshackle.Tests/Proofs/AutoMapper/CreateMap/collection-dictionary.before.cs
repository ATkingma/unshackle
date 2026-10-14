using System.Collections.Generic;
using AutoMapper;

namespace Shop;

public class Order
{
    public Dictionary<string, int> Totals { get; set; } = [];
}

public class OrderDto
{
    public Dictionary<string, int> Totals { get; set; } = [];
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
