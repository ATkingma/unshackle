using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class TimingBehavior<TRequest, TResponse>(int delay) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        return await next();
    }
}
