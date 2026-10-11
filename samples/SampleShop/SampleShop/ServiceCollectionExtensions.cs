using MediatR;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;
using SampleShop.Features.Customers;
using SampleShop.Features.Orders;
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
        services.AddSingleton<CustomerRepository>();
        services.AddTransient<OrderFacade>();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<OrderRepository>();
            cfg.RegisterGenericHandlers = true;
            cfg.NotificationPublisher = new TaskWhenAllPublisher();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddBehavior<IPipelineBehavior<RegisterCustomerCommand, Unit>, ValidationBehavior>();
            cfg.AddOpenStreamBehavior(typeof(StreamLoggingBehavior<,>));
            cfg.AddOpenRequestPreProcessor(typeof(AuditPreProcessor<>));
            cfg.AddOpenRequestPostProcessor(typeof(AuditPostProcessor<,>));
        });
        // Assembly scan: picks up OrderProfile and CustomerProfile.
        services.AddAutoMapper(typeof(OrderProfile).Assembly);
        return services;
    }
}
