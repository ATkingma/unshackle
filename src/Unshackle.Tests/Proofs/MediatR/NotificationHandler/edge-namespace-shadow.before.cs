namespace Proofs.Mediator
{
    public static class MediatorModule
    {
    }
}

namespace Proofs.Events
{
    using System.Threading;
    using System.Threading.Tasks;
    using MediatR;

    public record OrderPlaced(int Id) : INotification;

    public class OrderPlacedHandler : INotificationHandler<OrderPlaced>
    {
        public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
