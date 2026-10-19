using AutoMapper;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
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
            Customer = source.Customer,
        };
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
    }
}
