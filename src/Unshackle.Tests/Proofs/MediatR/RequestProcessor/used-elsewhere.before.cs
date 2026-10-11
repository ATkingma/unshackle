using System.Threading;
using System.Threading.Tasks;
using MediatR.Pipeline;

namespace Proofs;

public class AuditPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class Factory
{
    public static object Create() => new AuditPreProcessor<string>();
}
