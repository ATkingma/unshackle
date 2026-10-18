using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class LoginHandler;

public static class Startup
{
    public static void Configure(IServiceCollection services)
    {
        // Several assemblies named by typeof, as written in an ASP.NET host
        services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient);
    }
}
