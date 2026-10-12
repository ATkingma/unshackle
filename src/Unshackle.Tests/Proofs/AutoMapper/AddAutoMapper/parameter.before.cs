using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services, Assembly profiles)
    {
        services.AddAutoMapper(profiles);
        return services;
    }
}
