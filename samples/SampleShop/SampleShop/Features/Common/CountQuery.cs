using MediatR;
using SampleShop.Data;
using SampleShop.Domain;

namespace SampleShop.Features.Common;

// Generic request with an open generic handler (needs RegisterGenericHandlers).
public record CountQuery<TEntity>() : IRequest<int> where TEntity : class;

public class CountHandler<TEntity> : IRequestHandler<CountQuery<TEntity>, int> where TEntity : class
{
    private readonly OrderRepository _orders;
    private readonly CustomerRepository _customers;

    public CountHandler(OrderRepository orders, CustomerRepository customers)
    {
        _orders = orders;
        _customers = customers;
    }

    public Task<int> Handle(CountQuery<TEntity> request, CancellationToken cancellationToken) =>
        Task.FromResult(typeof(TEntity) == typeof(Order) ? _orders.All.Count : _customers.Customers.Count);
}
