using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class QueryHandler : IRequestHandler<Query, int>
{
    public async ValueTask<int> Handle(Query request, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        return request.Id;
    }
}
