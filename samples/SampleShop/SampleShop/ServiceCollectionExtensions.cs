using Microsoft.Extensions.DependencyInjection;
using SampleShop.Behaviors;
using SampleShop.Data;
using SampleShop.Mapping;

namespace SampleShop;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSampleShop(this IServiceCollection services)
    {
        services.AddSingleton<OrderRepository>();
        services.AddSingleton<AuditLog>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<OrderRepository>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });
        services.AddAutoMapper(typeof(OrderProfile));
        return services;
    }
}
