using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR.Pipeline;

namespace Proofs;

public class AuditPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    public List<string> Log { get; } = [];

    // Runs before the handler.
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        Log.Add($"Pre {typeof(TRequest).Name}");
        return Task.CompletedTask;
    }
}
