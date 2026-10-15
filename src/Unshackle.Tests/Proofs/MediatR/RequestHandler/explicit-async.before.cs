using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Job(int Id) : IRequest<Job>;

public abstract class JobHandler : IRequestHandler<Job, Job>
{
    public abstract Task Handle(Job request, CancellationToken ct);

    async Task<Job> IRequestHandler<Job, Job>.Handle(Job request, CancellationToken ct)
    {
        await Handle(request, ct);
        return request;
    }
}
