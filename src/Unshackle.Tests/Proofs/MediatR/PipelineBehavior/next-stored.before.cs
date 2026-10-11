using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class DeferBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        RequestHandlerDelegate<TResponse> later = next;
        return await later();
    }
}
