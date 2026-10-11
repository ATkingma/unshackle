using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.Pipeline;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public record Command(int Id) : IRequest;

public class QueryPreProcessor : IRequestPreProcessor<Query>
{
    public Task Process(Query request, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class CommandPreProcessor : IRequestPreProcessor<Command>
{
    public Task Process(Command request, CancellationToken cancellationToken) => Task.CompletedTask;
}
