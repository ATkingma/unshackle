using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Placed(int Id) : INotification;

public class PlacedHandler : INotificationHandler<Placed>
{
    public ValueTask Handle(Placed notification, CancellationToken cancellationToken)
    {
        if (notification.Id == 0)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(Task.Delay(notification.Id, cancellationToken));
    }
}
