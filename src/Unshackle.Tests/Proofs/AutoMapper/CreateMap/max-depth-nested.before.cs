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
        CreateMap<Order, OrderDto>().PreserveReferences();
        CreateMap<Address, AddressDto>();
    }
}
