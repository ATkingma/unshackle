#nullable enable
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

public static class OrderMapping
{
    public static OrderDto MapInto(this Order source, OrderDto destination)
    {
        destination.Id = source.Id;
        return destination;
    }
}

public class OrderService
{
    public OrderDto Update(IMapper mapper, Order order, OrderDto? dto) => mapper.Map(order, dto);
}
