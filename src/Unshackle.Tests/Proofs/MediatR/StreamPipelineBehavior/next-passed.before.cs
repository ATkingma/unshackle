using System.Collections.Generic;
using System.Threading;
using MediatR;

namespace Proofs;

public class StreamRetryBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => Run(next);

    private static IAsyncEnumerable<TResponse> Run(StreamHandlerDelegate<TResponse> action) => action();
}
