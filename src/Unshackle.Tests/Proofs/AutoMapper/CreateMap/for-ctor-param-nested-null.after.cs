using AutoMapper;

namespace Shop;

public class Customer
{
    public string? Name { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public Customer? Customer { get; set; }
}

public record OrderDto(int Id, string? CustomerName);

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new(source.Id, source.Customer?.Name);
}
