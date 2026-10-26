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

internal class OrderReader
{
    private readonly IMapper _mapper;

    public OrderReader(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TResult Read<TResult>(Order order) => _mapper.Map<TResult>(order);
}

internal class OrderEndpoints
{
    private readonly OrderReader _reader;

    public OrderEndpoints(OrderReader reader)
    {
        _reader = reader;
    }

    public OrderDto Details(Order order) => _reader.Read<OrderDto>(order);

    public OrderSummary Summary(Order order) => _reader.Read<OrderSummary>(order);
}
