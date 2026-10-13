using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public abstract class DomainEvent
{
    public bool IsPublished { get; set; }
}

public class AppointmentBookedEvent : DomainEvent
{
    public Guid AppointmentId { get; } = Guid.NewGuid();
}

public class AppointmentCompletedEvent : DomainEvent
{
}

public class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : DomainEvent
{
    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }

    public TDomainEvent DomainEvent { get; }
}

internal sealed class AppointmentBookedEventHandler : INotificationHandler<DomainEventNotification<AppointmentBookedEvent>>
{
    public Guid? Last { get; private set; }

    public Task Handle(DomainEventNotification<AppointmentBookedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        Last = domainEvent.AppointmentId;
        return Task.CompletedTask;
    }
}

public interface IDomainEventService
{
    Task Publish(DomainEvent domainEvent);
}

public class DomainEventService : IDomainEventService
{
    private readonly IPublisher _mediator;

    public DomainEventService(IPublisher mediator)
    {
        _mediator = mediator;
    }

    public Task Publish(DomainEvent domainEvent)
    {
        return _mediator.Publish(GetNotificationCorrespondingToDomainEvent(domainEvent));
    }

    private static INotification GetNotificationCorrespondingToDomainEvent(DomainEvent domainEvent)
    {
        return (INotification)Activator.CreateInstance(
            typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()), domainEvent)!;
    }
}