using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public interface IAppCommand : IRequest;

public interface IAppCommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : IAppCommand;

public record Command(int Id) : IAppCommand;

public class CommandHandler : IAppCommandHandler<Command>
{
    public async ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken)
    {
        await Task.Delay(request.Id, cancellationToken);
        return Unit.Value;
    }
}
