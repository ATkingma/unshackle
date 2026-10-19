using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public interface IOrderRepository
{
}

public class OrderService
{
    private readonly IOrderRepository _orders;

    public OrderService(IOrderRepository orders)
    {
        _orders = orders;
    }

    public IOrderRepository Orders => _orders;
}

public static class Registration
{
    public static IServiceCollection AddShop(this IServiceCollection services) => services.AddTransient<OrderService>();
}
