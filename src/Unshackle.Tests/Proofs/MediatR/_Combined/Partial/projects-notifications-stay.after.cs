// @project Domain
using Mediator;

namespace Shop.Domain;

public interface IDomainEvent : global::MediatR.INotification;

public record OrderPlaced(int Id) : IDomainEvent;

public record GetBrand(int Id) : IRequest<string>;
// @project Application -> Domain
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Shop.Domain;

namespace Shop.Application;

public class GetBrandHandler : IRequestHandler<GetBrand, string>
{
    public ValueTask<string> Handle(GetBrand request, CancellationToken cancellationToken) => ValueTask.FromResult("brand " + request.Id);
}

public class OrderPlacedHandler : global::MediatR.INotificationHandler<OrderPlaced>
{
    public Task Handle(OrderPlaced notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

// Wraps every notification handler: Mediator has no equivalent for this registration (the root).
public class Dispatching<TNotification> : global::MediatR.INotificationHandler<TNotification>
    where TNotification : global::MediatR.INotification
{
    private readonly global::MediatR.INotificationHandler<TNotification> _inner;

    public Dispatching(global::MediatR.INotificationHandler<TNotification> inner)
    {
        _inner = inner;
    }

    public Task Handle(TNotification notification, CancellationToken cancellationToken) => _inner.Handle(notification, cancellationToken);
}

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<global::MediatR.IMediator, global::MediatR.Mediator>();
        services.AddMediator(cfg =>
        {
            cfg.ServiceLifetime = ServiceLifetime.Scoped;
        });
        services.AddTransient<global::MediatR.INotificationHandler<OrderPlaced>, OrderPlacedHandler>();
        services.AddTransient(typeof(global::MediatR.INotificationHandler<>), typeof(Dispatching<>));
        return services;
    }
}
// @project Api -> Domain, Application
using System.Threading.Tasks;
using Mediator;
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
    private readonly global::MediatR.IPublisher _publisher;

    public EventDispatcher(global::MediatR.IPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Dispatch(IDomainEvent domainEvent) => _publisher.Publish(domainEvent);
}
