using AutoMapper;

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
            CreateMap<Address, AddressDto>();
        }
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
            CreateMap<Order, OrderDto>();
        }
    }
}
