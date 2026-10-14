using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>().BeforeMap((s, d) => d.Id = 1);
    }
}

public class OrderService
{
    public OrderDto Get(IMapper mapper, Order order) => mapper.Map<OrderDto>(order);
}
