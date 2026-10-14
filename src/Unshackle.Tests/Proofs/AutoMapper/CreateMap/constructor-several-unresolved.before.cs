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

    public OrderDto(int id, string comment)
    {
        Id = id;
        Comment = comment;
    }

    public int Id { get; }
    public string Name { get; set; } = "";
    public string Comment { get; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
