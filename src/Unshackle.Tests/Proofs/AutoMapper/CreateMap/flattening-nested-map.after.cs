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
    }
}

public static class OrderProfileExtensions
{
    public static OrderDto ToOrderDto(this Order source) => new()
    {
        CustomerAddress = (source.Customer?.Address)?.ToAddressDto(),
    };

    public static AddressDto ToAddressDto(this Address source) => new()
    {
        City = source.City,
    };
}
