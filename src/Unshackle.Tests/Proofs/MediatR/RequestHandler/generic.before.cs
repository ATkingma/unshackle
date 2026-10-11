using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Wrapped<T>(T Value) : IRequest<T>;

public class WrappedHandler<T> : IRequestHandler<Wrapped<T>, T>
{
    public Task<T> Handle(Wrapped<T> request, CancellationToken ct) => Task.FromResult(request.Value);
}
