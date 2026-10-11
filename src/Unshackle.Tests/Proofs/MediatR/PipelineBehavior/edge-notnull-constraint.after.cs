using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class TimingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(request, cancellationToken);
        return response;
    }
}
