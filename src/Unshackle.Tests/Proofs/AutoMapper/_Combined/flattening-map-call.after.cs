using AutoMapper;

namespace Shop;

public class Customer
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; } = new();

    public decimal GetTotal() => 10m;
}

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public int CustomerAge { get; set; }
    public decimal Total { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new()
    {
        Id = source.Id,
        CustomerName = source.Customer?.Name,
        CustomerAge = source.Customer?.Age ?? default,
        Total = source.GetTotal(),
    };
}

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public OrderDto Get(Order order) => order.ToOrderDto();
}
