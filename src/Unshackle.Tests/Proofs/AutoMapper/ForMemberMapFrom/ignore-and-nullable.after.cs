#nullable enable
using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public string Note { get; set; } = "";
}

public class OrderDto
{
    public int Id { get; set; }
    public string Note { get; set; } = "";
    public int Rank { get; set; }
    public string Label { get; set; } = "";
}

public class Line
{
    public int Quantity { get; set; }
}

public class LineDto
{
    public int Quantity { get; set; }
    public decimal Total { get; set; }
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
        Rank = source.Id,
        Label = source.Note.ToUpper(),
    };

    public static LineDto ToLineDto(this Line source) => new()
    {
        Quantity = source.Quantity,
    };
}
