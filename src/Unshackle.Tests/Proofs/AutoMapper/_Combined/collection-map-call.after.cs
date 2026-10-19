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

    public List<OrderDto> All(List<Order> orders) => orders?.Select(x => x?.ToOrderDto()).ToList() ?? [];

    public OrderDto[] Array(IEnumerable<Order> orders) => orders?.Select(x => x?.ToOrderDto()).ToArray() ?? [];

    public IEnumerable<OrderDto> Some(Order[] orders) => orders?.Select(x => x?.ToOrderDto()).ToList() ?? [];

    public int Count(IList<Order> orders) => (orders?.Select(x => x?.ToOrderDto()).ToList() ?? []).Count;
}
