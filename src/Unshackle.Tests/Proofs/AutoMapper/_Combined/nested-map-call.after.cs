using System.Collections.Generic;
using System.Linq;
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
    public List<Address> History { get; set; } = [];
}

public class OrderDto
{
    public int Id { get; set; }
    public AddressDto Address { get; set; } = new();
    public List<AddressDto> History { get; set; } = [];
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
        History = source.History?.Select(x => x?.ToAddressDto()).ToList() ?? [],
    };

    public static AddressDto ToAddressDto(this Address source) => new()
    {
        City = source.City,
    };
}

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public OrderDto Get(Order order) => order.ToOrderDto();
}
