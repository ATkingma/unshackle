using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public record CheckOrder(Guid OrderId);

public record OrderStatus(Guid OrderId, string Status);

public class OrderService
{
    private readonly IRequestClient<CheckOrder> _client;

    public OrderService(IRequestClient<CheckOrder> client) => _client = client;

    public async Task<string> StatusAsync(Guid id) => (await _client.GetResponse<OrderStatus>(new CheckOrder(id))).Message.Status;
}
