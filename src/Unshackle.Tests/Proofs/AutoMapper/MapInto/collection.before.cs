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

public class OrderService
{
    public List<OrderDto> Update(IMapper mapper, List<Order> orders, List<OrderDto> dtos) => mapper.Map(orders, dtos);
}
