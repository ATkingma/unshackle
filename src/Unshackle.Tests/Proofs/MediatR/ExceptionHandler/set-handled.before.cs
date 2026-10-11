using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.Pipeline;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class FallbackHandler : IRequestExceptionHandler<Query, int, KeyNotFoundException>
{
    public Task Handle(Query request, KeyNotFoundException exception, RequestExceptionHandlerState<int> state, CancellationToken cancellationToken)
    {
        if (request.Id < 0)
        {
            return Task.CompletedTask;
        }

        state.SetHandled(0);
        return Task.CompletedTask;
    }
}
