using AutoMapper;
using Shop.Mapping;

namespace Shop.Mapping
{
    public class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string Customer { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public class Customer
    {
        public string Name { get; set; } = "";
    }

    public class CustomerDto
    {
        public string Name { get; set; } = "";
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

        public static CustomerDto ToCustomerDto(this Customer source) => new()
        {
            Name = source.Name,
        };

        public static OrderDto MapInto(this Order source, OrderDto destination)
        {
            destination.Id = source.Id;
            destination.Customer = source.Customer;
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
