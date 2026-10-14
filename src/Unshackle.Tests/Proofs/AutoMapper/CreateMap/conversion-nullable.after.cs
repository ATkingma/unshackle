#nullable enable
using System;
using AutoMapper;

namespace Shop;

public enum Status { Open, Closed }

public class Order
{
    public int? Quantity { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime? ShippedAt { get; set; }
    public Status? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Rank { get; set; }
}

public class OrderDto
{
    public int Quantity { get; set; } = 1;
    public Guid CustomerId { get; set; }
    public DateTime ShippedAt { get; set; }
    public Status Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? Rank { get; set; }
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
        Quantity = source.Quantity.GetValueOrDefault(),
        CustomerId = source.CustomerId.GetValueOrDefault(),
        ShippedAt = source.ShippedAt.GetValueOrDefault(),
        Status = source.Status.GetValueOrDefault(),
        CreatedAt = source.CreatedAt,
        Rank = source.Rank,
    };
}
