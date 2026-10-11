using System.Threading;
using System.Threading.Tasks;
using MediatR.Pipeline;

namespace Proofs;

public class AuditPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        await Task.Yield();
    }
}
