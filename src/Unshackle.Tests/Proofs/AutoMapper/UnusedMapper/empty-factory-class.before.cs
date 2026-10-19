using AutoMapper;

namespace Proofs;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}

public static class MapperHelper
{
    public static IMapper Create() => new MapperConfiguration(cfg => cfg.CreateMap<Order, OrderDto>()).CreateMapper();
}

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public static OrderService Create() => new OrderService(MapperHelper.Create());
}
