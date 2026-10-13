using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class SilentPublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        return Task.CompletedTask;
    }
}
