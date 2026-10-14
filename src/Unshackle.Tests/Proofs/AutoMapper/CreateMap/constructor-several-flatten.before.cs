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

    public OrderDto(int id, string nameLength)
    {
        Id = id;
    }

    public int Id { get; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
