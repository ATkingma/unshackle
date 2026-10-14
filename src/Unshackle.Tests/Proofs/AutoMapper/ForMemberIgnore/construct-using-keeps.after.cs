using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class OrderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source)
    {
        OrderDto destination = new OrderDto { Label = "order " + source.Id };
        destination.Id = source.Id;
        return destination;
    }
}
