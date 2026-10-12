using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services) =>
        services.AddAutoMapper(typeof(Registration).Assembly);
}
