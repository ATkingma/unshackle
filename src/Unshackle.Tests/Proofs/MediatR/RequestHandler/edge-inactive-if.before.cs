using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class QueryHandler : IRequestHandler<Query, int>
{
    public Task<int> Handle(Query request, CancellationToken cancellationToken)
    {
#if (UseApiOnly)
        var message = "api only";
#endif
        return Task.FromResult(request.Id);
    }
}
