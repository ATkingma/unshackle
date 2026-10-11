using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Placed(int Id) : INotification;

public class PlacedHandler : INotificationHandler<Placed>
{
    public int Count { get; private set; }

    public ValueTask Handle(Placed notification, CancellationToken cancellationToken)
    {
        if (notification.Id < 0)
        {
            return ValueTask.CompletedTask;
        }

        Count += notification.Id;
        return ValueTask.CompletedTask;
    }
}
