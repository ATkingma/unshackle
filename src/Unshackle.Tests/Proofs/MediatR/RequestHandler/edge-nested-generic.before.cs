using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Result<T>(T Value);

public record Query(int Id) : IRequest<Result<List<int>>>;

public class QueryHandler : IRequestHandler<Query, Result<List<int>>>
{
    public async Task<Result<List<int>>> Handle(Query request, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        return new Result<List<int>>([request.Id]);
    }
}
