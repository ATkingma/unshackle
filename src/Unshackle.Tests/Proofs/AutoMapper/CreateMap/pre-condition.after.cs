using System;
using AutoMapper;

namespace Shop;

public class Order
{
    public int Id { get; set; }
    public bool Paid { get; set; }
    public decimal Amount { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public decimal PaidAmount { get; set; }
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
        PaidAmount = source.Paid ? source.Amount : default,
    };
}
