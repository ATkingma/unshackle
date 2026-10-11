using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Command(int Id) : IRequest;

public class CommandHandler : IRequestHandler<Command>
{
    public async Task Handle(Command request, CancellationToken cancellationToken) =>
        await Task.Delay(request.Id, cancellationToken);
}
