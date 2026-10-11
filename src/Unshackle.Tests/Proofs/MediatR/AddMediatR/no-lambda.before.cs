using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public static class Registration
{
    public static void AddShop(IServiceCollection services, MediatRServiceConfiguration configuration)
    {
        services.AddMediatR(configuration);
    }
}
