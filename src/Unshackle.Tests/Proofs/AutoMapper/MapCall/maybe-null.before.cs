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
    public static OrderDto ToOrderDto(this Order source) => new() { Id = source.Id };
}

public class OrderService
{
    public OrderDto? Find(IMapper mapper, Order? order) => mapper.Map<OrderDto?>(order);
}
