using AutoMapper;

namespace Shop;

public class Order
{
    public string CustomerId { get; set; } = "";
}

public class OrderDto
{
    public System.Guid CustomerId { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
