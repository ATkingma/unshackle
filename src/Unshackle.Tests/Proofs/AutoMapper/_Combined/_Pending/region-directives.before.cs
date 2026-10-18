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
        #region Orders
        CreateMap<Order, OrderDto>();
        #endregion

        #region Lines
        CreateMap<Line, LineDto>();
        #endregion
    }
}
