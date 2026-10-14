using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class OrderDto
{
    public OrderDto(int id)
    {
        Id = id;
    }

    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ConstructUsing(s => new OrderDto(s.Id * 10));
    }
}
