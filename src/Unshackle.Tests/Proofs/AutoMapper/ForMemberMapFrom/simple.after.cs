using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }
    public string Number { get; set; } = "";
}

public class OrderDto
{
    public int Id { get; set; }
    public long Total { get; set; }
    public string Reference { get; set; } = "";
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
        Id = source.Id,
        Total = source.Price * source.Quantity,
        Reference = "#" + source.Number,
    };
}
