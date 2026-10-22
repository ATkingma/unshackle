using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class Marker;

public class Other;

internal static class Registration
{
    // Every caller names a type of this project: the scan is this project, known at compile time (council round 8, A2)
    internal static IServiceCollection AddPipeline<TAssemblyOfType>(this IServiceCollection services)
    {
        services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient);
        return services;
    }
}

public static class Startup
{
    public static void Configure(IServiceCollection services) => services.AddPipeline<Marker>();

    public static void ConfigureAgain(IServiceCollection services) => services.AddPipeline<Other>();
}
