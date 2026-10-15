using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public static class Registration
{
    public static IServiceCollection AddPipeline<TAssemblyOfType>(this IServiceCollection services) =>
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<TAssemblyOfType>());
}
