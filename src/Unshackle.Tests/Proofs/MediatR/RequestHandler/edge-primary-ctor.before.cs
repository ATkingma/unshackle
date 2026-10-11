using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public interface IClock
{
    int Now { get; }
}

public record Query(int Id) : IRequest<int>;

public class QueryHandler(IClock clock) : IRequestHandler<Query, int>
{
    public Task<int> Handle(Query request, CancellationToken cancellationToken) => Task.FromResult(request.Id + clock.Now);
}
