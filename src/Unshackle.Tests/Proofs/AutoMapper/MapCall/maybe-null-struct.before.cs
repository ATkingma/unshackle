#nullable enable
using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public struct OrderDto
{
    public int Id { get; set; }
}

public static class OrderMapping
{
    public static OrderDto ToOrderDto(this Order source) => new() { Id = source.Id };
}

public class OrderService
{
    public void Send(IMapper mapper, Order? order)
    {
        var dto = mapper.Map<OrderDto>(order);
        Publish(dto);
    }

    private static void Publish(OrderDto dto)
    {
    }
}
