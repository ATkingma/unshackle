using AutoMapper;
using Shop.Customers;

namespace Shop.Customers
{
    public class Address
    {
        public string City { get; set; } = "";
    }

    public class AddressDto
    {
        public string City { get; set; } = "";
    }

    public class AddressProfile : Profile
    {
        public AddressProfile()
        {
        }
    }

    public static class AddressProfileExtensions
    {
        public static AddressDto ToAddressDto(this Address source) => new()
        {
            City = source.City,
        };
    }
}

namespace Shop.Orders
{
    public class Order
    {
        public int Id { get; set; }
        public Shop.Customers.Address Address { get; set; } = new();
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public Shop.Customers.AddressDto Address { get; set; } = new();
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
            Address = source.Address?.ToAddressDto(),
        };
    }
}
