using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        System.Console.WriteLine(typeof(TRequest).Name);
        return await next(request, cancellationToken);
    }
}
