using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public interface IAppQuery<out TResult> : IRequest<TResult>;

public interface IAppQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IAppQuery<TResult>;

public record Query(int Id) : IAppQuery<int>;

public class QueryHandler : IAppQueryHandler<Query, int>
{
    public async Task<int> Handle(Query request, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        return request.Id;
    }
}
