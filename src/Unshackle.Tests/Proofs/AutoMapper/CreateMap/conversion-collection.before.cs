using System.Collections.Generic;
using AutoMapper;

namespace Shop;

public enum Status { Open, Closed }

public class Order
{
    public List<int?> Quantities { get; set; } = [];
    public List<Status> History { get; set; } = [];
    public int[] Codes { get; set; } = [];
    public List<string> Amounts { get; set; } = [];
}

public class OrderDto
{
    public List<int> Quantities { get; set; } = [];
    public List<string> History { get; set; } = [];
    public long[] Codes { get; set; } = [];
    public IReadOnlyList<decimal> Amounts { get; set; } = [];
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
