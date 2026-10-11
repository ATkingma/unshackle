using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Command(int Id) : IRequest;

public class CommandHandler : IRequestHandler<Command>
{
    public int Count { get; private set; }

    public ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken)
    {
        Count += request.Id;
        return Unit.ValueTask;
    }
}
