using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record CountQuery<TEntity>() : IRequest<int> where TEntity : class;

public class CountHandler<TEntity> : IRequestHandler<CountQuery<TEntity>, int> where TEntity : class
{
    public Task<int> Handle(CountQuery<TEntity> request, CancellationToken cancellationToken) => Task.FromResult(0);
}
