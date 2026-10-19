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
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new()
    {
        Id = source.Id,
    };
}

public class OrderService
{

    public OrderService()
    {
    }

    // method group, not an invocation (eShopOnWeb PublicApi endpoints)
    public List<OrderDto> List(IEnumerable<Order> orders) => orders.Select(source => source.ToOrderDto()).ToList();
}
