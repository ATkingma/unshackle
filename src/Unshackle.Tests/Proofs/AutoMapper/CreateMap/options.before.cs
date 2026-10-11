using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>().ReverseMap();
    }
}
