using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public record OrderDto(int Id);

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
