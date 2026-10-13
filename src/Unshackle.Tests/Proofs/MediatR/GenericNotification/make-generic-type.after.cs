using System;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public abstract class DomainEvent
{
}

public class BookedEvent : DomainEvent
{
}

public class CancelledEvent : DomainEvent
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

public class DomainEventNotificationOfBookedEvent : DomainEventNotification<BookedEvent>
{
    public DomainEventNotificationOfBookedEvent(BookedEvent domainEvent)
        : base(domainEvent)
    {
    }
}

public class DomainEventNotificationOfCancelledEvent : DomainEventNotification<CancelledEvent>
{
    public DomainEventNotificationOfCancelledEvent(CancelledEvent domainEvent)
        : base(domainEvent)
    {
    }
}

public static class DomainEventNotificationTypes
{
    public static Type Close(Type type)
    {
        if (type == typeof(BookedEvent))
        {
            return typeof(DomainEventNotificationOfBookedEvent);
        }

        if (type == typeof(CancelledEvent))
        {
            return typeof(DomainEventNotificationOfCancelledEvent);
        }

        throw new ArgumentException("No concrete DomainEventNotification for " + type + ".", nameof(type));
    }
}

public class DomainEventService
{
    private readonly IPublisher _publisher;

    public DomainEventService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishAsync(DomainEvent domainEvent)
    {
        await _publisher.Publish(GetNotification(domainEvent));
    }

    private static INotification GetNotification(DomainEvent domainEvent)
    {
        return (INotification)Activator.CreateInstance(
            DomainEventNotificationTypes.Close(domainEvent.GetType()), domainEvent)!;
    }
}