using AutoMapper;
using NSubstitute;

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
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>();
    }
}

// The class under test: its only map is stubbed for any Customer (council A7) with the values the generated method copies from the one
// Customer the test builds, so it migrates.
public class CustomerService
{
    private readonly IMapper _mapper;

    public CustomerService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public CustomerDto Get(Customer customer) => _mapper.Map<CustomerDto>(customer);
}

public class CustomerServiceTests
{
    public string Get()
    {
        IMapper mapper = Substitute.For<IMapper>();
        var customer = new Customer { Id = 7, Name = "Ann" };
        var expected = new CustomerDto { Id = customer.Id, Name = "Ann" };
        mapper.Map<CustomerDto>(Arg.Any<Customer>()).Returns(expected);

        CustomerDto result = new CustomerService(mapper).Get(customer);

        return result.Name == expected.Name && result.Id == expected.Id ? "same" : "different";
    }
}
