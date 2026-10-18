using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Ping : IRequest<int>;

public class Expressions
{
    // A mocking library's Setup(m => m.Send(...)) lambda: the call is only described, never run, so it stays as is.
    public Expression<Func<IMediator, object>> Describe() => m => m.Send(new Ping(), CancellationToken.None);

    public Task<int> RunAsync(IMediator mediator) => mediator.Send(new Ping(), CancellationToken.None).AsTask();
}
