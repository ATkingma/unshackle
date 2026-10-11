using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public static class Registration
{
    public static void AddShop(IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);
            cfg.NotificationPublisher = new TaskWhenAllPublisher();
        });
    }
}
