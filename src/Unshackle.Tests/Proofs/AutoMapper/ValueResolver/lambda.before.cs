using System.Collections.Generic;
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
    public List<int> Numbers { get; set; } = [];
}

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public long Code { get; set; }
    public string Text { get; set; } = "";
    public List<int> Numbers { get; set; } = [];
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Numbers, o => o.Ignore())
            .ForMember(d => d.Text, o => o.MapFrom((s, d) => s.Id))
            .ForMember(d => d.Code, o => o.MapFrom((src, dest, member, ctx) => src.Id * 10))
            .ForMember(d => d.CustomerName, o => o.MapFrom((s, _) => s.Customer.Name));
    }
}
