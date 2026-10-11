using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Command(int Id) : IRequest;

public class CommandHandler : IRequestHandler<Command>
{
    public async ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken)
    {
        if (request.Id < 0)
        {
            return Unit.Value;
        }

        await Task.Delay(request.Id, cancellationToken);
        return Unit.Value;
    }
}
