using System.Threading;
using System.Threading.Tasks;
using MediatR.Pipeline;

namespace Proofs;

public class AuditPostProcessor : IRequestPostProcessor<string, int>
{
    public Task Process(string request, int response, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class AuditTests
{
    public Task Run(AuditPostProcessor processor)
    {
        return processor.Process("a", 1, CancellationToken.None);
    }
}
