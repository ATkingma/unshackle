using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class QueryHandler : IRequestHandler<Query, int>
{
    public ValueTask<int> Handle(Query request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(request.Id);
}
