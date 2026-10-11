using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public static class Registration
{
    public static void AddShop(IServiceCollection services)
    {
        services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient);
    }
}
