using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

// The generator only registers concrete messages: an abstract generic base is no "generic message".
public abstract class CommandBase<TResult> : IRequest<TResult>
{
}

public sealed class Ping : CommandBase<int>;

public class PingHandler : IRequestHandler<Ping, int>
{
    public ValueTask<int> Handle(Ping request, CancellationToken cancellationToken) => ValueTask.FromResult(42);
}
