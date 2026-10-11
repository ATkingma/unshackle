using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class AuditPreProcessor<TRequest>;

public static class Registration
{
    public static void AddShop(IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly);
            cfg.AddOpenRequestPreProcessor(typeof(AuditPreProcessor<>));
        });
    }
}
