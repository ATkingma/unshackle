using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

// The generator only registers concrete messages: an abstract generic base is no "generic message".
public abstract class CommandBase<TResult> : IRequest<TResult>
{
}

public sealed class Ping : CommandBase<int>;

public class PingHandler : IRequestHandler<Ping, int>
{
    public Task<int> Handle(Ping request, CancellationToken cancellationToken) => Task.FromResult(42);
}
