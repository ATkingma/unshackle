using System;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class OrderService
{
    public OrderService()
    {
    }
}

public static class Seeder
{
    public static OrderService? Seed(IServiceProvider services)
    {
        return new OrderService();
    }

    public static IServiceCollection AddShop(this IServiceCollection services) => services.AddAutoMapper(typeof(OrderService).Assembly);
}
