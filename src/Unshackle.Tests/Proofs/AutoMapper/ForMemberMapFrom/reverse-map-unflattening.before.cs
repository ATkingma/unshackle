#nullable enable
using AutoMapper;

namespace Shop;

public class Customer
{
    public string Name { get; set; } = "";
}

public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; } = new();
}

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.Name))
            .ReverseMap();
    }
}
