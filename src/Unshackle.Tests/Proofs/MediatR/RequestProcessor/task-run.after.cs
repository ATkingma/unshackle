using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class LoggingProcessor<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse> where TRequest : notnull, IMessage
{
    protected override ValueTask Handle(TRequest request, CancellationToken cancellationToken)
    {
        return new ValueTask(Task.Run(() => { _ = request.ToString(); }, cancellationToken));
    }
}
