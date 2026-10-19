using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Proofs
{
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

    public static class Registration
    {
        // An earlier use (kept out of the Scrutor scan): the reason still names the Decorate call.
        private static readonly Type[] s_decorators = [typeof(DispatchingDecorator<>)];

        public static bool IsDecorator(Type type) => Array.IndexOf(s_decorators, type) >= 0;

        public static IServiceCollection AddEvents(this IServiceCollection services) =>
            services.Decorate(typeof(INotificationHandler<>), typeof(DispatchingDecorator<>));
    }
}

// Stands in for Scrutor's ServiceCollectionExtensions.Decorate (fullcart): the namespace makes it resolve as Scrutor's (council round 4, Q4).
namespace Scrutor
{
    public static class ScrutorStandIn
    {
        public static IServiceCollection Decorate(this IServiceCollection services, Type serviceType, Type decoratorType) => services;
    }
}
