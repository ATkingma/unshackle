using AutoMapper;

namespace Shop;

public class Address
{
    public string Street { get; set; } = "";
}

public class Customer
{
    public int Id { get; set; }
    public Address Address { get; set; } = new();
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Street { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
    }
}

public static class OrderProfileExtensions
{
    public static CustomerDto ToCustomerDto(this Customer source) => new()
    {
        Id = source.Id,
    };

    public static Customer ToCustomer(this CustomerDto source) => new()
    {
        Id = source.Id,
        Address = new() { Street = source.Street },
    };
}
