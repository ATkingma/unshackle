using AutoMapper;

namespace Shop;

public class Customer
{
    public string Name { get; set; } = "";
}

public class Order
{
    public Customer Customer { get; set; } = new();
}

public class OrderDto
{
    public string CustomerName { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new()
    {
        CustomerName = source.Customer?.Name,
    };
}
