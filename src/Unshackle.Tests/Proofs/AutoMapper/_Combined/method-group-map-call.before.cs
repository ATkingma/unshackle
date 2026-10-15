using System.Collections.Generic;
using System.Linq;
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
        CreateMap<Order, OrderDto>();
    }
}

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }

    // method group, not an invocation (eShopOnWeb PublicApi endpoints)
    public List<OrderDto> List(IEnumerable<Order> orders) => orders.Select(_mapper.Map<OrderDto>).ToList();
}
