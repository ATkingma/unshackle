using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public record Command(int Id) : IRequest;

public class QueryPreProcessor : MessagePreProcessor<Query, int>
{
    protected override ValueTask Handle(Query request, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

public class CommandPreProcessor : MessagePreProcessor<Command, Unit>
{
    protected override ValueTask Handle(Command request, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
