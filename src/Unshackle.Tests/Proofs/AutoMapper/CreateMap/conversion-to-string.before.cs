#nullable enable
using System;
using AutoMapper;

namespace Shop;

public enum Status { Open, Closed }

public class Order
{
    public int Number { get; set; }
    public decimal Total { get; set; }
    public DateTime? ShippedAt { get; set; }
    public Guid Key { get; set; }
    public Uri? Site { get; set; }
    public Status? Status { get; set; }
    public bool Paid { get; set; }
}

public class OrderDto
{
    public string Number { get; set; } = "";
    public string Total { get; set; } = "";
    public string? ShippedAt { get; set; }
    public string Key { get; set; } = "";
    public string Site { get; set; } = "";
    public string? Status { get; set; }
    public string Paid { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
