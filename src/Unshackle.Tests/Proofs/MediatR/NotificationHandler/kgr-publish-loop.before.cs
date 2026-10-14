using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record MeetingCreated(int Id) : INotification;

// Shape from kgrzybek/modular-monolith-with-ddd (MIT): a handler next to a dispatcher that publishes in a loop.
public class MeetingCreatedHandler : INotificationHandler<MeetingCreated>
{
    public Task Handle(MeetingCreated notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class DomainEventsDispatcher
{
    private readonly IMediator _mediator;

    public DomainEventsDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchEventsAsync(IReadOnlyCollection<INotification> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent);
        }
    }
}
