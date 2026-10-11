using Microsoft.Extensions.DependencyInjection;
using Mediator;

namespace Proofs;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services) =>
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Transient);
}
