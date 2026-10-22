using AutoMapper;
using NSubstitute;

namespace Shop;

public class Customer
{
    public int Id { get; set; }
}

public class CustomerDto
{
    public int Id { get; set; }
}

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>();
    }
}

public class CustomerService
{
    private readonly IMapper _mapper;

    public CustomerService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public CustomerDto Get(Customer customer) => _mapper.Map<CustomerDto>(customer);
}

// Council A6: the test checks the Map call itself; no Received() equivalent remains for the generated method, so the mock and its class stay.
public class CustomerServiceTests
{
    public void Get()
    {
        IMapper mapper = Substitute.For<IMapper>();
        var customer = new Customer { Id = 7 };
        var expected = new CustomerDto { Id = 7 };
        mapper.Map<CustomerDto>(customer).Returns(expected);

        new CustomerService(mapper).Get(customer);

        mapper.Received(1).Map<CustomerDto>(customer);
    }
}
