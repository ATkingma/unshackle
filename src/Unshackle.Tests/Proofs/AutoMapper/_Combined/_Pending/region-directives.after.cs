using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public record OrderDto(int Id);

public class Line
{
    public int Qty { get; set; }
}

public record LineDto(int Qty);

public class ShopProfile : Profile
{
    public ShopProfile()
    {
    }
}

public static class ShopProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new(source.Id);

    public static LineDto ToLineDto(this Line source) => new(source.Qty);
}
