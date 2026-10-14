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

public class Order
{
    public Address Address { get; set; } = new();
}

public class OrderDto
{
    public AddressDto Address { get; set; } = new();
}

public class Customer
{
    public Address Address { get; set; } = new();
}

public class CustomerDto
{
    public AddressDto Address { get; set; } = new();
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<Address, AddressDto>();
    }
}

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>();
        CreateMap<Address, AddressDto>();
    }
}
