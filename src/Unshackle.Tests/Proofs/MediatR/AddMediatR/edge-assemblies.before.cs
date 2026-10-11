using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

internal static class Registration
{
    internal static IServiceCollection AddShop(this IServiceCollection services, Assembly handlers)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(handlers));
        return services;
    }
}
