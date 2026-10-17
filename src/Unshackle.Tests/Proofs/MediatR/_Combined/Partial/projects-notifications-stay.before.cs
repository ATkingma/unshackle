// @project Domain
using MediatR;

namespace Shop.Domain;

public interface IDomainEvent : INotification;

public record OrderPlaced(int Id) : IDomainEvent;

public record GetBrand(int Id) : IRequest<string>;
// @project Application -> Domain
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Shop.Domain;

namespace Shop.Application;

public class GetBrandHandler : IRequestHandler<GetBrand, string>
{
    public Task<string> Handle(GetBrand request, CancellationToken cancellationToken) => Task.FromResult("brand " + request.Id);
}

public class OrderPlacedHandler : INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

// Wraps every notification handler: Mediator has no equivalent for this registration (the root).
public class Dispatching<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    private readonly INotificationHandler<TNotification> _inner;

    public Dispatching(INotificationHandler<TNotification> inner)
    {
        _inner = inner;
    }

    public Task Handle(TNotification notification, CancellationToken cancellationToken) => _inner.Handle(notification, cancellationToken);
}

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddTransient<INotificationHandler<OrderPlaced>, OrderPlacedHandler>();
        services.AddTransient(typeof(INotificationHandler<>), typeof(Dispatching<>));
        return services;
    }
}
// @project Api -> Domain, Application
using System.Threading.Tasks;
using MediatR;
using Shop.Domain;

namespace Shop.Api;

public class BrandController
{
    private readonly ISender _sender;

    public BrandController(ISender sender)
    {
        _sender = sender;
    }

    public async Task<string> Get(int id) => await _sender.Send(new GetBrand(id));
}

public class EventDispatcher
{
    private readonly IPublisher _publisher;

    public EventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Dispatch(IDomainEvent domainEvent) => _publisher.Publish(domainEvent);
}
