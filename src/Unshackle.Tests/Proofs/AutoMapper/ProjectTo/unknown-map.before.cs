using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace Shop;

public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}

public class OrderQueries
{
    private readonly IMapper _mapper;

    public OrderQueries(IMapper mapper)
    {
        _mapper = mapper;
    }

    public OrderDto? First(IQueryable<Order> orders) => orders.ProjectTo<OrderDto>(_mapper.ConfigurationProvider).FirstOrDefault();
}
