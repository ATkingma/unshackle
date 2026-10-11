using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class QueryHandler : IRequestHandler<Query, int>
{
    public async ValueTask<int> Handle(Query query, CancellationToken token)
    {
        await Task.Yield();
        return query.Id;
    }
}
