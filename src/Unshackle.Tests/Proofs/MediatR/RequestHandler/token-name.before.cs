using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class QueryHandler : IRequestHandler<Query, int>
{
    public async Task<int> Handle(Query query, CancellationToken token)
    {
        await Task.Yield();
        return query.Id;
    }
}
