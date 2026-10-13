using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ardalis.SharedKernel
{
  /// <summary>
  /// Publishes domain events through Mediator. Keeps its MediatR name, so registrations and references stay the same.
  /// </summary>
  public class MediatRDomainEventDispatcher : IDomainEventDispatcher
  {
    private readonly global::Mediator.IMediator _mediator;

    public MediatRDomainEventDispatcher(global::Mediator.IMediator mediator)
    {
      _mediator = mediator;
    }

    public async Task DispatchAndClearEvents(IEnumerable<EntityBase> entitiesWithEvents)
    {
      foreach (var entity in entitiesWithEvents)
      {
        var events = entity.DomainEvents.ToArray();
        entity.ClearDomainEvents();
        foreach (var domainEvent in events)
        {
          await _mediator.Publish(domainEvent).ConfigureAwait(false);
        }
      }
    }
  }
}
