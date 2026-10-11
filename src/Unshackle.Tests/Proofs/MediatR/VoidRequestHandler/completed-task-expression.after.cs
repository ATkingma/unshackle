using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Command(int Id) : IRequest;

public class CommandHandler : IRequestHandler<Command>
{
    public ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken) => Unit.ValueTask;
}
