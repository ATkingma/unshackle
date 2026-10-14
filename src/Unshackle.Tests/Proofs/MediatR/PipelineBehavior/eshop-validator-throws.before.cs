using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public interface IValidator<in T>
{
    IEnumerable<string> Validate(T instance);
}

// Shape from dotnet/eShop (MIT): ValidatorBehavior throws before calling next().
public class ValidatorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidatorBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = _validators.SelectMany(v => v.Validate(request)).ToList();
        if (failures.Any())
        {
            throw new InvalidOperationException($"Command Validation Errors for type {typeof(TRequest).Name}");
        }

        return await next();
    }
}
