using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public int? Quantity { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public int Quantity { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}

public class OrderQueries
{
    private readonly IMapper _mapper;

    public OrderQueries(IMapper mapper)
    {
        _mapper = mapper;
    }

    public List<OrderDto> All(IQueryable<Order> orders) => orders.ProjectTo<OrderDto>(_mapper.ConfigurationProvider).ToList();
}
