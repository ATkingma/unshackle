using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ardalis.SharedKernel
{
  /// <summary>
  /// A simple interface for sending domain events. Can use Mediator or any other implementation.
  /// </summary>
  public interface IDomainEventDispatcher
  {
    Task DispatchAndClearEvents(IEnumerable<IHasDomainEvents> entitiesWithEvents);
  }
}
