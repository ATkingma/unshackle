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

// The class under test never reads what Map returns, and its test mocks IMapper without stubs (council A8): it migrates, with an Info line.
public class CustomerService
{
    private readonly IMapper _mapper;

    public CustomerService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public int Save(Customer customer)
    {
        _ = _mapper.Map<CustomerDto>(customer);
        return customer.Id;
    }
}

public class CustomerServiceTests
{
    public int Save()
    {
        IMapper mapper = Substitute.For<IMapper>();
        var customer = new Customer { Id = 7, Name = "Ann" };

        return new CustomerService(mapper).Save(customer);
    }
}
