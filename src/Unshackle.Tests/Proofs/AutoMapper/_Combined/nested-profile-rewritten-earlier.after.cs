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
    public Address Home { get; set; } = new();
}

public class OrderDto
{
    public int Id { get; set; }
    public AddressDto Shipping { get; set; } = new();
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
        Shipping = source.Home?.ToAddressDto(),
    };
}

public class OrderService
{
    public OrderService()
    {
    }

    public OrderDto Get(Order order) => order.ToOrderDto();
}
