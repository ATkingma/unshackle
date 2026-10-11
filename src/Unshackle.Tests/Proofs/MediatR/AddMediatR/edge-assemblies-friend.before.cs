using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Proofs.Tests")]

namespace Proofs;

internal static class Registration
{
    internal static IServiceCollection AddShop(this IServiceCollection services, Assembly handlers)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(handlers));
        return services;
    }
}
