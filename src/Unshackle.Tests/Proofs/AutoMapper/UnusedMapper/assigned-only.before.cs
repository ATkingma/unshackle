using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public interface IOrderRepository
{
}

public class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IMapper _mapper;

    public OrderService(IOrderRepository orders, IMapper mapper)
    {
        _orders = orders;
        _mapper = mapper;
    }

    public IOrderRepository Orders => _orders;
}

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services) => services.AddTransient<OrderService>();
}
