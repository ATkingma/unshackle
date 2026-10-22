using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public record OrderSubmitted(Guid OrderId);

// Council round 11, T7: reading headers keeps the consumer Manual (ConsumerHeaders).
public class OrderConsumer : IConsumer<OrderSubmitted>
{
    public Task Consume(ConsumeContext<OrderSubmitted> context) => context.Headers.Get<string>("tenant") is null ? Task.CompletedTask : Task.CompletedTask;
}
