using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new()
    {
        Quantities = source.Quantities?.Select(x => x.GetValueOrDefault()).ToList() ?? [],
        History = source.History?.Select(x => x.ToString()).ToList() ?? [],
        Codes = source.Codes?.Select(x => (long)x).ToArray() ?? [],
        Amounts = source.Amounts?.Select(x => Convert.ToDecimal(x)).ToList() ?? [],
    };
}
