using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Shop;

public class Order
{
    public List<string> Lines { get; set; } = [];
}

public class OrderDto
{
    public List<string> Lines { get; set; } = [];
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
        Lines = source.Lines?.ToList() ?? [],
    };
}
