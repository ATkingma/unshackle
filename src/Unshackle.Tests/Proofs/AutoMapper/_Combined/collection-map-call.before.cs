using System.Collections.Generic;
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

    public List<OrderDto> All(List<Order> orders) => _mapper.Map<List<OrderDto>>(orders);

    public OrderDto[] Array(IEnumerable<Order> orders) => _mapper.Map<OrderDto[]>(orders);

    public IEnumerable<OrderDto> Some(Order[] orders) => _mapper.Map<IEnumerable<OrderDto>>(orders);

    public int Count(IList<Order> orders) => _mapper.Map<IList<OrderDto>>(orders).Count;
}
