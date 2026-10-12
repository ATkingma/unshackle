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
    public OrderDto Update(IMapper mapper, Order order, OrderDto dto) => mapper.Map(order, dto);
}
