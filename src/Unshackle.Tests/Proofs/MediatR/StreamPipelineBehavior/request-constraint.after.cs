using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class StreamLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TRequest, TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in next(request, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}
