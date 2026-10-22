using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public record OrderSubmitted(Guid OrderId);

// Council round 11, T7: responding keeps the consumer Manual (ConsumerRespond).
public class OrderConsumer : IConsumer<OrderSubmitted>
{
    public Task Consume(ConsumeContext<OrderSubmitted> context) => context.RespondAsync(context.Message);
}
