using System;

namespace Ardalis.SharedKernel
{
  /// <summary>
  /// A base type for domain events. Depends on Mediator INotification.
  /// Includes DateOccurred which is set on creation.
  /// </summary>
  public abstract class DomainEventBase : global::Mediator.INotification
  {
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
  }
}
