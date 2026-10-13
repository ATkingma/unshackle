using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class SilentPublisher : IPublisher
{
    public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => default;

    public ValueTask Publish(object notification, CancellationToken cancellationToken = default) => default;
}
