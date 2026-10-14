using AutoMapper;
using System;

namespace Shop;

public enum Status { Open, Closed }

public class OrderForm
{
    public string Quantity { get; set; } = "";
    public string Price { get; set; } = "";
    public string Paid { get; set; } = "";
    public string OrderedAt { get; set; } = "";
    public string Status { get; set; } = "";
}

public class Order
{
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public bool Paid { get; set; }
    public System.DateTime OrderedAt { get; set; }
    public Status Status { get; set; }
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static Order ToOrder(this OrderForm source) => new()
    {
        Quantity = Convert.ToInt32(source.Quantity),
        Price = Convert.ToDecimal(source.Price),
        Paid = Convert.ToBoolean(source.Paid),
        OrderedAt = Convert.ToDateTime(source.OrderedAt),
        Status = string.IsNullOrEmpty(source.Status) ? default : Enum.Parse<Status>(source.Status, true),
    };
}
