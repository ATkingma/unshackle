using System;
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

public class OrderSummary
{
    public int Id { get; set; }
}

public static class OrderMapping
{
    public static Expression<Func<Order, OrderDto>> OrderToOrderDtoProjection => source => new OrderDto { Id = source.Id };

    public static Expression<Func<Order, OrderSummary>> OrderToOrderSummaryProjection => source => new OrderSummary { Id = source.Id };
}

internal class OrderReader
{
    private readonly IMapper _mapper;

    public OrderReader(IMapper mapper)
    {
        _mapper = mapper;
    }

    public IQueryable<TResult> Query<TResult>(IQueryable<Order> orders) => _mapper.ProjectTo<TResult>(orders);
}

internal class OrderEndpoints
{
    private readonly OrderReader _reader;

    public OrderEndpoints(OrderReader reader)
    {
        _reader = reader;
    }

    public IQueryable<OrderDto> Details(IQueryable<Order> orders) => _reader.Query<OrderDto>(orders);

    public IQueryable<OrderSummary> Summaries(IQueryable<Order> orders) => _reader.Query<OrderSummary>(orders);
}
