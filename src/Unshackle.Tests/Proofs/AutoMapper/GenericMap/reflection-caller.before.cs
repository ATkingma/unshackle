using System;
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

internal class OrderReader
{
    private readonly IMapper _mapper;

    public OrderReader(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TResult Read<TResult>(Order order) => _mapper.Map<TResult>(order);

    public OrderDto Details(Order order) => Read<OrderDto>(order);

    public object ReadAs(Type type, Order order) =>
        typeof(OrderReader).GetMethod(nameof(Read))!.MakeGenericMethod(type).Invoke(this, [order])!;
}
