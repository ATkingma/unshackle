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
    public static OrderDto MapInto(this Order source, OrderDto destination)
    {
        destination.Id = source.Id;
        return destination;
    }
}

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public OrderDto Update(Order order, OrderDto dto) => _mapper.Map(order, dto);

    public void Fill(Order[] orders, OrderDto dto) => _mapper.Map<Order, OrderDto>(orders[0], dto);
}
