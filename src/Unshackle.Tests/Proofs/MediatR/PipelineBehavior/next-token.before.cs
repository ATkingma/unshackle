using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class TokenBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) => next(ct);
}
