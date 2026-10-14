using AutoMapper;

namespace Shop;

public class Order
{
    public long? Version { get; set; }
}

public class OrderDto
{
    public int? Version { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
