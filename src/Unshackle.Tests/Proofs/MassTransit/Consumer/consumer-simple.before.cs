using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public record OrderSubmitted(Guid OrderId);

// Council round 11, T7: only context.Message and the cancellation token, no definition or filter: an Info row.
public class OrderConsumer : IConsumer<OrderSubmitted>
{
    public Task Consume(ConsumeContext<OrderSubmitted> context) => Task.CompletedTask;
}
