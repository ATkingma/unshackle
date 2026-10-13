using System;
using System.Threading.Tasks;
using MediatR;

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

public class DomainEventNotification<TDomainEvent> : INotification
    where TDomainEvent : DomainEvent
{
    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }

    public TDomainEvent DomainEvent { get; }
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
            typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()), domainEvent)!;
    }
}