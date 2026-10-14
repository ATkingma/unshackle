using AutoMapper;

namespace Shop;

public class Address
{
    public string City { get; set; } = "";
}

public class AddressDto
{
    public string City { get; set; } = "";
}

public class Customer
{
    public Address Address { get; set; } = new();
}

public class Order
{
    public Customer Customer { get; set; } = new();
}

public class OrderDto
{
    public AddressDto CustomerAddress { get; set; } = new();
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<Address, AddressDto>();
    }
}
