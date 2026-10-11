using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class TimingBehavior<TRequest, TResponse>(int delay) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        return await next(request, cancellationToken);
    }
}
