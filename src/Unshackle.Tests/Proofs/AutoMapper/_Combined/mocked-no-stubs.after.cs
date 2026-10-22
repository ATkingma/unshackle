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
    }
}

public static class CustomerProfileExtensions
{
    public static CustomerDto ToCustomerDto(this Customer source) => new()
    {
        Id = source.Id,
        Name = source.Name,
    };
}

// The class under test never reads what Map returns, and its test mocks IMapper without stubs (council A8): it migrates, with an Info line.
public class CustomerService
{
    public CustomerService()
    {
    }

    public int Save(Customer customer)
    {
        _ = customer.ToCustomerDto();
        return customer.Id;
    }
}

public class CustomerServiceTests
{
    public int Save()
    {
        IMapper mapper = Substitute.For<IMapper>();
        var customer = new Customer { Id = 7, Name = "Ann" };

        return new CustomerService().Save(customer);
    }
}
