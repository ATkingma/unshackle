using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class DelayPreProcessor<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse> where TRequest : notnull, IMessage
{
    protected override ValueTask Handle(TRequest request, CancellationToken cancellationToken) => new ValueTask(Task.Delay(1, cancellationToken));
}
