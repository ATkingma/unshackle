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

// The class under test: its test stubs IMapper, so it keeps calling IMapper.
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
    public CustomerDto Get()
    {
        IMapper mapper = Substitute.For<IMapper>();
        mapper.Map<CustomerDto>(Arg.Any<Customer>()).Returns(new CustomerDto { Id = 7 });
        return new CustomerService(mapper).Get(new Customer());
    }
}
