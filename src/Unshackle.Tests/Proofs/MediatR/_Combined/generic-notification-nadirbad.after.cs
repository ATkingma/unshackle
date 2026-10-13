using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

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

public abstract class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : DomainEvent
{
    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }

    public TDomainEvent DomainEvent { get; }
}

public class DomainEventNotificationOfAppointmentBookedEvent : DomainEventNotification<AppointmentBookedEvent>
{
    public DomainEventNotificationOfAppointmentBookedEvent(AppointmentBookedEvent domainEvent)
        : base(domainEvent)
    {
    }
}

public class DomainEventNotificationOfAppointmentCompletedEvent : DomainEventNotification<AppointmentCompletedEvent>
{
    public DomainEventNotificationOfAppointmentCompletedEvent(AppointmentCompletedEvent domainEvent)
        : base(domainEvent)
    {
    }
}

public static class DomainEventNotificationTypes
{
    public static Type Close(Type type)
    {
        if (type == typeof(AppointmentBookedEvent))
        {
            return typeof(DomainEventNotificationOfAppointmentBookedEvent);
        }

        if (type == typeof(AppointmentCompletedEvent))
        {
            return typeof(DomainEventNotificationOfAppointmentCompletedEvent);
        }

        throw new ArgumentException("No concrete DomainEventNotification for " + type + ".", nameof(type));
    }
}

internal sealed class AppointmentBookedEventHandler : INotificationHandler<DomainEventNotification<AppointmentBookedEvent>>
{
    public Guid? Last { get; private set; }

    public ValueTask Handle(DomainEventNotification<AppointmentBookedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        Last = domainEvent.AppointmentId;
        return ValueTask.CompletedTask;
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
        return _mediator.Publish(GetNotificationCorrespondingToDomainEvent(domainEvent)).AsTask();
    }

    private static INotification GetNotificationCorrespondingToDomainEvent(DomainEvent domainEvent)
    {
        return (INotification)Activator.CreateInstance(
            DomainEventNotificationTypes.Close(domainEvent.GetType()), domainEvent)!;
    }
}