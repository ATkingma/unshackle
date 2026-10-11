using System.Collections.Generic;
using System.Threading;
using Mediator;

namespace Proofs;

public class StreamLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse> where TRequest : notnull, IStreamMessage
{
    public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken) => next(request, cancellationToken);
}
