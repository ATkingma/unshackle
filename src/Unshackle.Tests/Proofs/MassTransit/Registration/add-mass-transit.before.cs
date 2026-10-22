using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public record OrderSubmitted(Guid OrderId);

public class OrderConsumer : IConsumer<OrderSubmitted>
{
    public Task Consume(ConsumeContext<OrderSubmitted> context) => Task.CompletedTask;
}

public static class Bus
{
    public static void Register(IServiceCollection services) =>
        services.AddMassTransit(x => x.AddConsumer<OrderConsumer>());
}
