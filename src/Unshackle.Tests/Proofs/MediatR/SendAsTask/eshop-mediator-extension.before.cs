using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

// Shape from dotnet/eShop (MIT): an extension on IMediator that collects the Publish tasks.
public static class MediatorExtension
{
    public static Task DispatchDomainEventsAsync(this IMediator mediator, IEnumerable<INotification> domainEvents)
    {
        List<Task> tasks = domainEvents.Select(domainEvent => mediator.Publish(domainEvent)).ToList();
        return Task.WhenAll(tasks);
    }
}
