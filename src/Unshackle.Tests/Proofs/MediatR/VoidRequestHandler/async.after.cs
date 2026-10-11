using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Command(int Id) : IRequest;

public class CommandHandler : IRequestHandler<Command>
{
    public async ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken)
    {
        await Task.Delay(request.Id, cancellationToken);
        return Unit.Value;
    }
}
