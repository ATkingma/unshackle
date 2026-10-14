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

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}

// Kavita's ProjectToExtensions: parameters fill the captured variables of MapFrom lambdas per call.
public static class ProjectToExtensions
{
    public static IQueryable<OrderDto> ProjectToWithUser(this IQueryable<Order> queryable, IMapper mapper, int userId) =>
        queryable.ProjectTo<OrderDto>(mapper.ConfigurationProvider, new { userId });
}
