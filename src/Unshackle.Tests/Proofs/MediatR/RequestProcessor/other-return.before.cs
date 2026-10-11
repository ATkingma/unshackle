using System.Threading;
using System.Threading.Tasks;
using MediatR.Pipeline;

namespace Proofs;

public class DelayPreProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken cancellationToken) => Task.Delay(1, cancellationToken);
}
