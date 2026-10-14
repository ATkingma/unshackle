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
    public int Id { get; set; }
    public Address Address { get; set; } = new();
}

public class OrderDto
{
    public int Id { get; set; }
    public AddressDto Address { get; set; } = new();
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

    public static AddressDto ToAddressDto(this Address source) => new()
    {
        City = source.City,
    };
}
