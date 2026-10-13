using System.Collections.Generic;

namespace Ardalis.SharedKernel
{
  public interface IHasDomainEvents
  {
    IReadOnlyCollection<DomainEventBase> DomainEvents { get; }
  }
}
