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

// The class under test: its only map is stubbed with the value the generated method returns, so it migrates.
public class CustomerService
{
    public CustomerService()
    {
    }

    public CustomerDto Get(Customer customer) => customer.ToCustomerDto();
}

public class CustomerServiceTests
{
    public string Get()
    {
        IMapper mapper = Substitute.For<IMapper>();
        var customer = new Customer { Id = 7, Name = "Ann" };
        var expected = new CustomerDto { Id = customer.Id, Name = "Ann" };

        CustomerDto result = new CustomerService().Get(customer);

        return result.Name == expected.Name && result.Id == expected.Id ? "same" : "different";
    }
}
