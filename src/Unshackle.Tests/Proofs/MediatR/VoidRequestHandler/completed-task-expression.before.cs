using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Command(int Id) : IRequest;

public class CommandHandler : IRequestHandler<Command>
{
    public Task Handle(Command request, CancellationToken cancellationToken) => Task.CompletedTask;
}
