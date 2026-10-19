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

    // Handed out as it is: the IMapper stays, so does its registration.
    public IMapper Mapper => _mapper;
}

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services)
    {
        services.AddTransient<OrderService>();
        services.AddAutoMapper(typeof(OrderService).Assembly);
        return services;
    }
}
