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
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public OrderDto Get(Order order) => _mapper.Map<OrderDto>(order);

    public OrderDto First(Order[] orders) => this._mapper.Map<Order, OrderDto>(orders[0]);

    public OrderDto Created(int id) => _mapper.Map<OrderDto>(new Order { Id = id });
}
