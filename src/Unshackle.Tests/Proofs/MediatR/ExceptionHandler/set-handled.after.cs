using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class FallbackHandler : MessageExceptionHandler<Query, int, KeyNotFoundException>
{
    protected override ValueTask<ExceptionHandlingResult<int>> Handle(Query request, KeyNotFoundException exception, CancellationToken cancellationToken)
    {
        if (request.Id < 0)
        {
            return new(NotHandled);
        }

        return new(Handled(0));
    }
}
