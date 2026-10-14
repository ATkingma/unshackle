using AutoMapper;

namespace Shop;

public class Order
{
    public string Quantity { get; set; } = "";
}

public class OrderDto
{
    public int? Quantity { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
