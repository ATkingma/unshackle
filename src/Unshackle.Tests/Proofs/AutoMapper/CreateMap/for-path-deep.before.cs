using System;
using AutoMapper;

namespace Shop;

public class Country
{
    public string Code { get; set; } = "";
}

public class Address
{
    public Country Country { get; set; } = new();
}

public class Customer
{
    public Address Address { get; set; } = new();
}

public class CustomerForm
{
    public string CountryCode { get; set; } = "";
}

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<CustomerForm, Customer>()
            .ForPath(d => d.Address.Country.Code, o => o.MapFrom(s => s.CountryCode));
    }
}
