using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        System.Console.WriteLine(typeof(TRequest).Name);
        return await next();
    }
}
