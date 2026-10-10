using MediatR;
using SampleShop.Data;

namespace SampleShop.Features.Orders;

public class OrderPlacedHandler : INotificationHandler<OrderPlaced>
{
    private readonly AuditLog _log;

    public OrderPlacedHandler(AuditLog log)
    {
        _log = log;
    }

    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
    {
        _log.Entries.Add($"Order {notification.OrderId} placed");
        return Task.CompletedTask;
    }
}
