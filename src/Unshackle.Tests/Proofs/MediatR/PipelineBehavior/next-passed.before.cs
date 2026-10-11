using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) =>
        Retry(() => next());

    private static Task<TResponse> Retry(Func<Task<TResponse>> action) => action();
}
