using System.Threading;
using System.Threading.Tasks;
using MediatR.Pipeline;

namespace Proofs;

public class AuditTests
{
    public async Task Run(IRequestPostProcessor<string, int> processor)
    {
        await processor.Process("a", 1, CancellationToken.None);
    }
}
