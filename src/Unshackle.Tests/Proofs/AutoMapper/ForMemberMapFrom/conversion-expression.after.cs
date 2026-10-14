using AutoMapper;
using System;

namespace Shop;

public class Order
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal? Discount { get; set; }
    public int First { get; set; }
    public int Second { get; set; }
}

public class OrderDto
{
    public int Total { get; set; }
    public decimal Rebate { get; set; }
    public string Label { get; set; } = "";
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
        Total = Convert.ToInt32(source.Price * source.Quantity),
        Rebate = source.Discount.GetValueOrDefault(),
        Label = (source.First + source.Second).ToString(),
    };
}
