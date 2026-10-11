using MediatR;
using SampleShop.Features.Customers;

namespace SampleShop.Behaviors;

// Second behavior, closed over one request type.
public class ValidationBehavior : IPipelineBehavior<RegisterCustomerCommand, Unit>
{
    public Task<Unit> Handle(RegisterCustomerCommand request, RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ArgumentException("Last name is required.", nameof(request));
        }

        return next();
    }
}
