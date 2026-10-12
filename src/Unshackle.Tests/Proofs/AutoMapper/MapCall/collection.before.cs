using System.Collections.Generic;
using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}

public class OrderService
{
    public List<OrderDto> All(IMapper mapper, List<Order> orders) => mapper.Map<List<OrderDto>>(orders);
}
