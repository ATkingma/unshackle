using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class Dispatcher
{
    private readonly IMediator _mediator;

    public Dispatcher(IMediator mediator) => _mediator = mediator;

    public Task<object?> DispatchAsync(object message, CancellationToken cancellationToken)
    {
        return _mediator.Send(message, cancellationToken).AsTask();
    }
}
