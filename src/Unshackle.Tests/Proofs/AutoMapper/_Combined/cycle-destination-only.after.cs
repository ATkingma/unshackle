using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;

namespace Shop;

// BoxModel.Order closes a type cycle OrderModel → BoxModel → OrderModel, but BoxDto has no Order: AutoMapper never follows it.
public class OrderModel
{
    public Guid OrderId { get; set; }
    public ICollection<BoxModel> Boxes { get; set; } = new List<BoxModel>();
}

public class BoxModel
{
    public Guid OrderId { get; set; }
    public OrderModel? Order { get; set; }
    public string Type { get; set; } = "";
}

public class OrderDto
{
    public Guid OrderId { get; set; }
    public List<BoxDto> Boxes { get; set; } = [];
}

public class BoxDto
{
    public Guid OrderId { get; set; }
    public string Type { get; set; } = "";
}

public class OrderMapping : Profile
{
    public OrderMapping()
    {
    }
}

public static class OrderMappingExtensions
{
    public static OrderModel ToOrderModel(this OrderDto source) => new()
    {
        OrderId = source.OrderId,
        Boxes = source.Boxes?.Select(x => x?.ToBoxModel()).ToList() ?? [],
    };

    public static OrderDto ToOrderDto(this OrderModel source) => new()
    {
        OrderId = source.OrderId,
        Boxes = source.Boxes?.Select(x => x?.ToBoxDto()).ToList() ?? [],
    };

    public static BoxDto ToBoxDto(this BoxModel source) => new()
    {
        OrderId = source.OrderId,
        Type = source.Type,
    };

    public static BoxModel ToBoxModel(this BoxDto source) => new()
    {
        OrderId = source.OrderId,
        Type = source.Type,
    };
}

public class OrderService
{
    public OrderService()
    {
    }

    public OrderModel Add(OrderDto order) => order.ToOrderModel();

    public OrderDto Get(OrderModel order) => order.ToOrderDto();
}
