using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;

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
    public static Expression<Func<Order, OrderDto>> OrderToOrderDtoProjection => source => new OrderDto { Id = source.Id };
}

public class OrderQueries
{
    private readonly IMapper _mapper;

    public OrderQueries(IMapper mapper)
    {
        _mapper = mapper;
    }

    public List<OrderDto> All(IQueryable<Order> orders) =>
        orders
            .Where(o => o.Id > 0)
            .Select(OrderMapping.OrderToOrderDtoProjection)
            .ToList();

    public IQueryable<OrderDto> Query(IQueryable<Order> orders) => orders.Select(OrderMapping.OrderToOrderDtoProjection);
}
