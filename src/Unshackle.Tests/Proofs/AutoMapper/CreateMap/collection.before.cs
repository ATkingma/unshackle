using System.Collections.Generic;
using AutoMapper;

namespace Shop;

public class Order
{
    public List<string> Lines { get; set; } = [];
}

public class OrderDto
{
    public List<string> Lines { get; set; } = [];
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
