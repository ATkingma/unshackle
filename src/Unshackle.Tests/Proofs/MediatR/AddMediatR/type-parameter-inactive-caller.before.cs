using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class Marker;

internal static class Registration
{
    internal static IServiceCollection AddPipeline<TAssemblyOfType>(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<TAssemblyOfType>());
        return services;
    }
}

public static class Startup
{
    public static void Configure(IServiceCollection services)
    {
        services.AddPipeline<Marker>();
#if LEGACY
        services.AddPipeline<string>();
#endif
    }
}
