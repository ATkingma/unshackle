using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);
            cfg.RegisterGenericHandlers = true;
        });
        return services;
    }
}
