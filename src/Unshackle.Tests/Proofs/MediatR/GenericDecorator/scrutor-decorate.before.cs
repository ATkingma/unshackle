using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class DispatchingDecorator<T> : INotificationHandler<T>
    where T : INotification
{
    private readonly INotificationHandler<T> _decorated;

    public DispatchingDecorator(INotificationHandler<T> decorated)
    {
        _decorated = decorated;
    }

    public Task Handle(T notification, CancellationToken cancellationToken) => _decorated.Handle(notification, cancellationToken);
}

// Stands in for Scrutor's ServiceCollectionExtensions.Decorate (fullcart).
public static class ScrutorStandIn
{
    public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Type decoratorType) => services;
}

public static class Registration
{
    // An earlier use (kept out of the Scrutor scan): the reason still names the Decorate call.
    private static readonly Type[] s_decorators = [typeof(DispatchingDecorator<>)];

    public static bool IsDecorator(Type type) => Array.IndexOf(s_decorators, type) >= 0;

    public static IServiceCollection AddEvents(this IServiceCollection services) =>
        services.Decorate(typeof(INotificationHandler<>), typeof(DispatchingDecorator<>));
}
