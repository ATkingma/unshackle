using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Command(int Id) : IRequest;

public class CommandHandler : IRequestHandler<Command>
{
    public int Count { get; private set; }

    public Task Handle(Command request, CancellationToken cancellationToken)
    {
        Count += request.Id;
        return Task.CompletedTask;
    }
}
