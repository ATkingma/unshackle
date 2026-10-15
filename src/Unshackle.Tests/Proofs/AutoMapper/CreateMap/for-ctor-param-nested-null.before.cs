using AutoMapper;

namespace Shop;

public class Customer
{
    public string? Name { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public Customer? Customer { get; set; }
}

public record OrderDto(int Id, string? CustomerName);

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForCtorParam("customerName", o => o.MapFrom(s => s.Customer!.Name));
    }
}
