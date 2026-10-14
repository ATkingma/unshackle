using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class OrderDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ConstructUsing(s => new OrderDto { Label = "order " + s.Id })
            .ForMember(d => d.Name, o => o.Ignore());
    }
}
