using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Shop;

public class Order;

// Generic message: Mediator's generator has none (the root). Only CountController sends it.
public record CountQuery<TEntity>() : IRequest<int> where TEntity : class;

public class CountHandler<TEntity> : IRequestHandler<CountQuery<TEntity>, int> where TEntity : class
{
    public Task<int> Handle(CountQuery<TEntity> request, CancellationToken cancellationToken) => Task.FromResult(1);
}

public record GetBrand(int Id) : IRequest<string>;

public class GetBrandHandler : IRequestHandler<GetBrand, string>
{
    public Task<string> Handle(GetBrand request, CancellationToken cancellationToken) => Task.FromResult("brand " + request.Id);
}

public class CountController
{
    private readonly ISender _sender;

    public CountController(ISender sender)
    {
        _sender = sender;
    }

    public Task<int> Count() => _sender.Send(new CountQuery<Order>());
}

public class BrandController
{
    private readonly ISender _sender;

    public BrandController(ISender sender)
    {
        _sender = sender;
    }

    public async Task<string> Get(int id) => await _sender.Send(new GetBrand(id));
}

public static class DependencyInjection
{
    public static IServiceCollection AddShop(this IServiceCollection services)
    {
        services.AddTransient<IMediator, Mediator>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.AddTransient<IRequestHandler<CountQuery<Order>, int>, CountHandler<Order>>();
        services.AddTransient<IRequestHandler<GetBrand, string>, GetBrandHandler>();
        return services;
    }
}
