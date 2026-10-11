using MediatR;
using MediatR.Pipeline;
using SampleShop.Data;

namespace SampleShop.Features.Orders;

// IRequest without a response (void handler).
public record CancelOrderCommand(int Id) : IRequest;

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly OrderRepository _repository;
    private readonly IPublisher _publisher;

    public CancelOrderHandler(OrderRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = _repository.Find(request.Id) ?? throw new KeyNotFoundException($"Order {request.Id} not found.");
        order.Cancel();
        await _publisher.Publish(new OrderCancelled(request.Id), cancellationToken);
    }
}

// Two handlers for one notification: run in parallel by the TaskWhenAllPublisher strategy.
public record OrderCancelled(int OrderId) : INotification;

public class OrderCancelledAuditHandler : INotificationHandler<OrderCancelled>
{
    private readonly AuditLog _log;

    public OrderCancelledAuditHandler(AuditLog log)
    {
        _log = log;
    }

    public Task Handle(OrderCancelled notification, CancellationToken cancellationToken)
    {
        lock (_log.Entries)
        {
            _log.Entries.Add($"Order {notification.OrderId} cancelled");
        }

        return Task.CompletedTask;
    }
}

public class OrderCancelledMailHandler : INotificationHandler<OrderCancelled>
{
    private readonly AuditLog _log;

    public OrderCancelledMailHandler(AuditLog log)
    {
        _log = log;
    }

    public async Task Handle(OrderCancelled notification, CancellationToken cancellationToken)
    {
        await Task.Yield();
        lock (_log.Entries)
        {
            _log.Entries.Add($"Mail sent for order {notification.OrderId}");
        }
    }
}

// Exception handler: an unknown order becomes a handled result instead of an exception.
public class OrderNotFoundExceptionHandler : IRequestExceptionHandler<CancelOrderCommand, Unit, KeyNotFoundException>
{
    private readonly AuditLog _log;

    public OrderNotFoundExceptionHandler(AuditLog log)
    {
        _log = log;
    }

    public Task Handle(CancelOrderCommand request, KeyNotFoundException exception, RequestExceptionHandlerState<Unit> state, CancellationToken cancellationToken)
    {
        _log.Entries.Add($"Not found: {request.Id}");
        state.SetHandled(Unit.Value);
        return Task.CompletedTask;
    }
}
