using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class OrderProfile : Profile
{
}

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services)
    {
        services.AddSingleton<OrderProfile>();
        services.AddAutoMapper(typeof(OrderProfile).Assembly);
        return services;
    }
}
