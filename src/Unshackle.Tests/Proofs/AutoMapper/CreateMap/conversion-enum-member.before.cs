using System.Runtime.Serialization;
using AutoMapper;

namespace Shop;

public enum Status
{
    [EnumMember(Value = "open")]
    Open,
    [EnumMember(Value = "closed")]
    Closed,
}

public class Order
{
    public Status Status { get; set; }
}

public class OrderDto
{
    public string Status { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
