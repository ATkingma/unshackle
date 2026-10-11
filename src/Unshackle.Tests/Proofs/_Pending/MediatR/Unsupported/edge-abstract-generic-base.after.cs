using Mediator;

namespace Proofs;

public abstract class CommandBase<TResult> : IRequest<TResult>
{
}

public sealed class Ping : CommandBase<int>;
