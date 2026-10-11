using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public class Facade
{
    private readonly ISender? _sender;

    public Facade(ISender? sender)
    {
        _sender = sender;
    }

    public Task<int>? GetAsync(int id, CancellationToken cancellationToken) => _sender?.Send(new Query(id), cancellationToken);
}
