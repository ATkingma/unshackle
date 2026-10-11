using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class TokenBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    public ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken ct) => next(request, ct);
}
