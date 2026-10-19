using AutoMapper;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
    }

    public record OrderDto(int Id, string Customer);

    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
        }
    }

    public static class OrderProfileExtensions
    {
        public static OrderDto ToOrderDto(this Order source) => new(source.Id, source.Customer);
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
