using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Placed(int Id) : INotification;

public class PlacedHandler : INotificationHandler<Placed>
{
    Task INotificationHandler<Placed>.Handle(Placed notification, CancellationToken cancellationToken) => Task.CompletedTask;
}
