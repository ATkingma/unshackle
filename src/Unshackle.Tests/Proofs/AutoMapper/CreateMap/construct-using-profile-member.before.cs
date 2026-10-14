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
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ConstructUsing(s => Make(s));
    }

    private static OrderDto Make(Order order) => new() { Id = order.Id };
}
