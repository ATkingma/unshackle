using System.Runtime.CompilerServices;
using MediatR;
using SampleShop.Data;

namespace SampleShop.Behaviors;

public class StreamLoggingBehavior<TRequest, TResponse> : IStreamPipelineBehavior<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    private readonly AuditLog _log;

    public StreamLoggingBehavior(AuditLog log)
    {
        _log = log;
    }

    public async IAsyncEnumerable<TResponse> Handle(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _log.Entries.Add($"Streaming {typeof(TRequest).Name}");
        await foreach (var item in next().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}
