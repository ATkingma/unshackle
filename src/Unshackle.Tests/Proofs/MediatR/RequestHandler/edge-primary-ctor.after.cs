using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public interface IClock
{
    int Now { get; }
}

public record Query(int Id) : IRequest<int>;

public class QueryHandler(IClock clock) : IRequestHandler<Query, int>
{
    public ValueTask<int> Handle(Query request, CancellationToken cancellationToken) => ValueTask.FromResult(request.Id + clock.Now);
}
