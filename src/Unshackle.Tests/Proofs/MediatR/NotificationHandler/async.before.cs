using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Placed(int Id) : INotification;

public class PlacedHandler : INotificationHandler<Placed>
{
    public async Task Handle(Placed notification, CancellationToken cancellationToken)
    {
        await Task.Delay(notification.Id, cancellationToken);
    }
}
