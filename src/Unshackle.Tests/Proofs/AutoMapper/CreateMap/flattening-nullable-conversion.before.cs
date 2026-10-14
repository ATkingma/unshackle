using AutoMapper;

namespace Shop;

public class Customer
{
    public int Age { get; set; }
}

public class Order
{
    public Customer Customer { get; set; } = new();
}

public class OrderDto
{
    public long? CustomerAge { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
