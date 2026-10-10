using SampleShop.Domain;

namespace SampleShop.Data;

public class OrderRepository
{
    private readonly Dictionary<int, Order> _orders = [];

    public IReadOnlyDictionary<int, Order> All => _orders;

    public IQueryable<Order> Query() => _orders.Values.AsQueryable();

    public Order? Find(int id) => _orders.GetValueOrDefault(id);

    public int Add(Order order)
    {
        order.Id = _orders.Count + 1;
        _orders.Add(order.Id, order);
        return order.Id;
    }
}
