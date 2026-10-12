using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class QueryHandler : IRequestHandler<Query, int>
{
    public ValueTask<int> Handle(Query request, CancellationToken cancellationToken)
    {
#if (UseApiOnly)
        var message = "api only";
#endif
        return ValueTask.FromResult(request.Id);
    }
}
