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

public class OrderSummary
{
    public int Id { get; set; }
}

public static class OrderMapping
{
    public static Expression<Func<Order, OrderDto>> OrderToOrderDtoProjection => source => new OrderDto { Id = source.Id };

    public static Expression<Func<Order, OrderSummary>> OrderToOrderSummaryProjection => source => new OrderSummary { Id = source.Id };
}

// The paging helper of cats (ProjectToPaginatedDataAsync), internal: every caller is in the solution.
internal static class QueryableExtensions
{
    public static List<TResult> ProjectToPage<T, TResult>(this IQueryable<T> query, IConfigurationProvider configuration, int pageNumber, int pageSize) =>
        query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(typeof(T) == typeof(Order) && typeof(TResult) == typeof(OrderDto) ? (Expression<Func<T, TResult>>)(object)OrderMapping.OrderToOrderDtoProjection
                : typeof(T) == typeof(Order) && typeof(TResult) == typeof(OrderSummary) ? (Expression<Func<T, TResult>>)(object)OrderMapping.OrderToOrderSummaryProjection
                : throw new global::System.InvalidOperationException($"No generated projection for {typeof(T)}, {typeof(TResult)}."))
            .ToList();
}

internal class OrderQueries
{
    private readonly IMapper _mapper;

    public OrderQueries(IMapper mapper)
    {
        _mapper = mapper;
    }

    public List<OrderDto> Details(IQueryable<Order> orders) => orders.ProjectToPage<Order, OrderDto>(_mapper.ConfigurationProvider, 1, 10);

    public List<OrderSummary> Summaries(IQueryable<Order> orders) => orders.ProjectToPage<Order, OrderSummary>(_mapper.ConfigurationProvider, 1, 10);
}
