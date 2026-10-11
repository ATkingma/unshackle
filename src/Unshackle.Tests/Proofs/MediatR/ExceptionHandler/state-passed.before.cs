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
        Mark(state);
        return Task.CompletedTask;
    }

    private static void Mark(RequestExceptionHandlerState<int> state) => state.SetHandled(0);
}
