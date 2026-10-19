using System;
using AutoMapper;
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
        var mapper = services.GetService<IMapper>();
        if (mapper == null)
        {
            Console.WriteLine("AutoMapper is not registered.");
            return null;
        }

        return new OrderService();
    }

    public static IServiceCollection AddShop(this IServiceCollection services, Action<Action<IServiceCollection>> later)
    {
        later(s => s.AddAutoMapper(typeof(OrderService).Assembly));
        return services;
    }
}
