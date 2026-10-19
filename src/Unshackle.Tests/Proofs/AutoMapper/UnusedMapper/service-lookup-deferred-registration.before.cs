using System;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
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

        return new OrderService(mapper);
    }

    public static IServiceCollection AddShop(this IServiceCollection services, Action<Action<IServiceCollection>> later)
    {
        later(s => s.AddAutoMapper(typeof(OrderService).Assembly));
        return services;
    }
}
