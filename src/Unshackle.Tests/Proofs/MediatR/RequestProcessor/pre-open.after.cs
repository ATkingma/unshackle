using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class AuditPreProcessor<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse> where TRequest : notnull, IMessage
{
    public List<string> Log { get; } = [];

    // Runs before the handler.
    protected override ValueTask Handle(TRequest request, CancellationToken cancellationToken)
    {
        Log.Add($"Pre {typeof(TRequest).Name}");
        return ValueTask.CompletedTask;
    }
}
