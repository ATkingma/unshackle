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

    public OrderDto(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new(source.Id, source.Name);
}
