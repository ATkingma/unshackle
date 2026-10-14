using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class OrderDto
{
    private OrderDto(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public OrderDto(int id)
    {
        Id = id;
    }

    public int Id { get; }
    public string Name { get; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
