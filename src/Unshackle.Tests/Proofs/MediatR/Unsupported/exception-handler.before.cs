using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.Pipeline;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class FallbackHandler : IRequestExceptionHandler<Query, int, InvalidOperationException>
{
    public Task Handle(Query request, InvalidOperationException exception, RequestExceptionHandlerState<int> state, CancellationToken cancellationToken)
    {
        state.SetHandled(0);
        return Task.CompletedTask;
    }
}

public class LogAction : IRequestExceptionAction<Query, InvalidOperationException>
{
    public Task Execute(Query request, InvalidOperationException exception, CancellationToken cancellationToken) => Task.CompletedTask;
}
