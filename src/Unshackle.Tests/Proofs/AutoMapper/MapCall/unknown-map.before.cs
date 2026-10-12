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
    public OrderDto Get(IMapper mapper, Order order) => mapper.Map<OrderDto>(order);
}
