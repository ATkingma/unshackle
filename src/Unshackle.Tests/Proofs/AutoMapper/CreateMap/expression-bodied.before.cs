using AutoMapper;

namespace Shop;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class CustomerProfile : Profile
{
    public CustomerProfile() => CreateMap<Customer, CustomerDto>();
}
