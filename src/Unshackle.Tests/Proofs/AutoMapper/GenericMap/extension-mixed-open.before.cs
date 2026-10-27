using System;
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

public static class OrderMapping
{
    public static OrderDto ToOrderDto(this Order source) => new() { Id = source.Id };
}

internal static class OrderReading
{
    public static TResult ReadAs<TResult>(this IMapper mapper, Order order) => mapper.Map<TResult>(order);
}

internal class Reader<TDto>
{
    private readonly IMapper _mapper;

    public Reader(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TDto Read(Order order) => _mapper.ReadAs<TDto>(order);
}

internal static class Registrations
{
    public static Dictionary<Type, Type> Services() => new() { [typeof(Reader<>)] = typeof(Reader<>) };

    public static OrderDto Details(IMapper mapper, Order order) => OrderReading.ReadAs<OrderDto>(mapper, order);
}
