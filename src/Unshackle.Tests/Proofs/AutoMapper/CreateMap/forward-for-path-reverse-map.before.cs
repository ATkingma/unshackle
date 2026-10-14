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
        CreateMap<CustomerDto, Customer>()
            .ForPath(customer => customer.Address.Street, opt => opt.MapFrom(dto => dto.Street))
            .ReverseMap();
    }
}
