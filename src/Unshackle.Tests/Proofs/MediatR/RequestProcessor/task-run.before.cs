using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.Pipeline;

namespace Proofs;

public class LoggingProcessor<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        return Task.Run(() => { _ = request.ToString(); }, cancellationToken);
    }
}
