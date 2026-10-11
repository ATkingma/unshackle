using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class QueryHandler : IRequestHandler<Query, int>
{
    Task<int> IRequestHandler<Query, int>.Handle(Query request, CancellationToken cancellationToken) =>
        Task.FromResult(request.Id);
}
