using AutoMapper;

namespace Shop;

public class Order
{
    public decimal Total { get; set; }
    public string number = "";

    public decimal GetTotal() => Total * 2;

    public decimal GetTax() => Total / 5;

    public string Code() => "C-" + number;
}

public class OrderDto
{
    public decimal Total { get; set; }
    public decimal Tax { get; set; }
    public string Number { get; set; } = "";
    public string Code { get; set; } = "";
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
        Total = source.Total,
        Tax = source.GetTax(),
        Number = source.number,
        Code = source.Code(),
    };
}
