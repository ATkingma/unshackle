using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class SequentialPublisher : INotificationPublisher
{
    public async Task Publish(IEnumerable<NotificationHandlerExecutor> handlerExecutors, INotification notification, CancellationToken cancellationToken)
    {
        foreach (NotificationHandlerExecutor executor in handlerExecutors)
        {
            await executor.HandlerCallback(notification, cancellationToken);
        }
    }
}
