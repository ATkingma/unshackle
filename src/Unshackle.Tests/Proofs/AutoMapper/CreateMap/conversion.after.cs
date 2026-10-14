using AutoMapper;

namespace Shop;

public enum Status { Open, Closed }

public class Order
{
    public Status Status { get; set; }
}

public class OrderDto
{
    public string Status { get; set; } = "";
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
        Status = source.Status.ToString(),
    };
}
