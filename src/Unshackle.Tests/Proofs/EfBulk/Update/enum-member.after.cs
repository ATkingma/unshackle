#nullable enable
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public enum OrderKind
{
    Sale,
    Return,
}

public class Order
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public string? Note { get; set; }
    public int Retries { get; set; }
    public bool Open { get; set; }
    public OrderKind Kind { get; set; }
}

public class ShopContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
}

public class Request
{
    public int Limit { get; set; }
}

public class Cleanup
{
    public int Run(ShopContext db) => db.Orders.ExecuteUpdate(setters => setters.SetProperty(o => o.Kind, o => OrderKind.Return));
}
