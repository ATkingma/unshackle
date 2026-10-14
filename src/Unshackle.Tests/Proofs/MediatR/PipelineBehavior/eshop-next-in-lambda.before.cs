using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public interface IExecutionStrategy
{
    Task ExecuteAsync(Func<Task> operation);
}

// Shape from dotnet/eShop (MIT): TransactionBehavior calls next() inside an execution-strategy lambda.
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IExecutionStrategy _strategy;

    public TransactionBehavior(IExecutionStrategy strategy)
    {
        _strategy = strategy;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = default(TResponse);
        if (request is null)
        {
            return await next();
        }

        await _strategy.ExecuteAsync(async () =>
        {
            response = await next();
        });

        return response!;
    }
}
