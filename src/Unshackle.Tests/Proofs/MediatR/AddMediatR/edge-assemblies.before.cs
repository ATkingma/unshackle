using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services, Assembly handlers)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(handlers));
        return services;
    }
}
