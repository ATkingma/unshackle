using AutoMapper;
using Shop.Mapping;
using System;

namespace Shop.Mapping
{
    public enum Status { Open, Closed }

    public class Order
    {
        public int? Quantity { get; set; }
        public Status Status { get; set; }
        public double Weight { get; set; }
    }

    public class OrderDto
    {
        public int Quantity { get; set; }
        public string Status { get; set; } = "";
        public int Weight { get; set; }
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
            Status = source.Status.ToString(),
            Weight = Convert.ToInt32(source.Weight),
        };

        public static OrderDto MapInto(this Order source, OrderDto destination)
        {
            destination.Quantity = source.Quantity.GetValueOrDefault();
            destination.Status = source.Status.ToString();
            destination.Weight = Convert.ToInt32(source.Weight);
            return destination;
        }
    }
}

namespace Shop.Orders
{
    public class OrderService
    {
        public OrderService()
        {
        }

        public Mapping.OrderDto Get(Mapping.Order order) => order.ToOrderDto();

        public void Update(Mapping.Order order, Mapping.OrderDto dto)
        {
            order.MapInto(dto);
        }
    }
}
