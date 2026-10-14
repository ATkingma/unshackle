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
    public int Scale { get; set; }
}

public class Options
{
    public int Scale { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile(Options options)
    {
        CreateMap<Order, OrderDto>()
            .ConstructUsing(s => new OrderDto { Scale = options.Scale });
    }
}
