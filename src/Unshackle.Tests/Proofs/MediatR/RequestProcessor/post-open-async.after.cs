using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class AuditPostProcessor<TRequest, TResponse> : MessagePostProcessor<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    protected override async ValueTask Handle(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        await Task.Yield();
    }
}
