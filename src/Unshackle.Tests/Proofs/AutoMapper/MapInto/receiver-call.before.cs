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
    public IMapper Mapper() => null!;

    public OrderDto Update(Order order, OrderDto dto) => Mapper().Map(order, dto);
}
