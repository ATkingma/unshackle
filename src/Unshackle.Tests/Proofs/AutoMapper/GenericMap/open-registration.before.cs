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

internal class Reader<TDto>
{
    private readonly IMapper _mapper;

    public Reader(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TDto Read(Order order) => _mapper.Map<TDto>(order);
}

internal static class Registrations
{
    public static Dictionary<Type, Type> Services() => new() { [typeof(Reader<>)] = typeof(Reader<>) };

    public static Reader<OrderDto> Orders(IMapper mapper) => new(mapper);
}
