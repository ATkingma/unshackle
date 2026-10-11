using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services, Assembly handlers)
    {
        _ = handlers;
        services.AddMediator(configuration => configuration.ServiceLifetime = ServiceLifetime.Transient);
        return services;
    }
}
