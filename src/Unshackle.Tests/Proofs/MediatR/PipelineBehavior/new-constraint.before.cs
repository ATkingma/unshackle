using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class DefaultsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, new()
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) =>
        await next();
}
