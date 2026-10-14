using AutoMapper;

namespace Shop;

public class Address
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
    public Address CustomerAddress { get; set; } = new();
}

public class OrderDto
{
    public string CustomerAddressCity { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
