using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Command(int Id) : IRequest;

public class CommandHandler : IRequestHandler<Command>
{
    public async Task Handle(Command request, CancellationToken cancellationToken)
    {
        if (request.Id < 0)
        {
            return;
        }

        await Task.Delay(request.Id, cancellationToken);
    }
}
