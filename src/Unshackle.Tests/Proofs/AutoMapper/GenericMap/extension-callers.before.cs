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

public class OrderSummary
{
    public int Id { get; set; }
}

public static class OrderMapping
{
    public static OrderDto ToOrderDto(this Order source) => new() { Id = source.Id };

    public static OrderSummary ToOrderSummary(this Order source) => new() { Id = source.Id };
}

internal static class OrderReading
{
    public static TResult ReadAs<TResult>(this IMapper mapper, Order order) => mapper.Map<TResult>(order);
}

internal class OrderEndpoints
{
    private readonly IMapper _mapper;

    public OrderEndpoints(IMapper mapper)
    {
        _mapper = mapper;
    }

    public OrderDto Details(Order order) => _mapper.ReadAs<OrderDto>(order);

    public OrderSummary Summary(Order order) => _mapper.ReadAs<OrderSummary>(order);
}
