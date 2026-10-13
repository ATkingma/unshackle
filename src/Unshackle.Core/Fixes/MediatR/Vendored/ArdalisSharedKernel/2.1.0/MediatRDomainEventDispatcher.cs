using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ardalis.SharedKernel
{
  /// <summary>
  /// Publishes domain events through Mediator. Keeps its MediatR name, so registrations and references stay the same.
  /// </summary>
  public class MediatRDomainEventDispatcher : IDomainEventDispatcher
  {
    private readonly global::Mediator.IMediator _mediator;
    private readonly ILogger<MediatRDomainEventDispatcher> _logger;

    public MediatRDomainEventDispatcher(global::Mediator.IMediator mediator, ILogger<MediatRDomainEventDispatcher> logger)
    {
      _mediator = mediator;
      _logger = logger;
    }

    public async Task DispatchAndClearEvents(IEnumerable<IHasDomainEvents> entitiesWithEvents)
    {
      foreach (IHasDomainEvents entity in entitiesWithEvents)
      {
        if (entity is HasDomainEventsBase hasDomainEvents)
        {
          DomainEventBase[] events = hasDomainEvents.DomainEvents.ToArray();
          hasDomainEvents.ClearDomainEvents();

          foreach (DomainEventBase domainEvent in events)
            await _mediator.Publish(domainEvent).ConfigureAwait(false);
        }
        else
        {
          _logger.LogError(
            "Entity of type {EntityType} does not inherit from {BaseType}. Unable to clear domain events.",
            entity.GetType().Name,
            nameof(HasDomainEventsBase));
        }
      }
    }
  }
}
