using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class DefaultsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IMessage, new()
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken) =>
        await next(request, cancellationToken);
}
