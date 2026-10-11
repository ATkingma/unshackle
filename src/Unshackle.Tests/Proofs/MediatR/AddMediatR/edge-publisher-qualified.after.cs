using Microsoft.Extensions.DependencyInjection;
using Mediator;

namespace Proofs;

public static class Registration
{
    public static void AddShop(IServiceCollection services)
    {
        services.AddMediator(cfg =>
        {
            cfg.ServiceLifetime = ServiceLifetime.Transient;
            cfg.NotificationPublisherType = typeof(global::Mediator.ForeachAwaitPublisher);
        });
    }
}
