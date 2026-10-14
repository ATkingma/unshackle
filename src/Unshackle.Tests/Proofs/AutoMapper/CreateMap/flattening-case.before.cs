using AutoMapper;

namespace Shop;

public class Order
{
    public string Name { get; set; } = "";
    public string name = "";
}

public class OrderDto
{
    public string NAME { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
