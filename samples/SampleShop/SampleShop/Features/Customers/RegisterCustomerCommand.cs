using MediatR;
using SampleShop.Data;
using SampleShop.Domain;

namespace SampleShop.Features.Customers;

// Unit return, written out explicitly.
public record RegisterCustomerCommand(string FirstName, string LastName, string City) : IRequest<Unit>;

public class RegisterCustomerHandler : IRequestHandler<RegisterCustomerCommand, Unit>
{
    private readonly CustomerRepository _repository;

    public RegisterCustomerHandler(CustomerRepository repository)
    {
        _repository = repository;
    }

    public Task<Unit> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        _repository.Customers.Add(new Customer
        {
            Id = _repository.Customers.Count + 1,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Address = new Address { City = request.City },
            CreatedAt = new DateTime(2026, 1, 1),
        });
        return Task.FromResult(Unit.Value);
    }
}
