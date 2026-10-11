using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Wrapped<T>(T Value) : IRequest<T>;

public class WrappedHandler<T> : IRequestHandler<Wrapped<T>, T>
{
    public ValueTask<T> Handle(Wrapped<T> request, CancellationToken ct) => ValueTask.FromResult(request.Value);
}
