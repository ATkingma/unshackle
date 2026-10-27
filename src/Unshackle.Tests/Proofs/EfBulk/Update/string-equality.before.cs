#nullable enable
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;

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
    private const string Closed = "closed";

    public int Run(ShopContext db) => db.Orders.Update(o => new Order { Open = o.Note == "new" });

    public int Reopen(ShopContext db) => db.Orders.Update(o => new Order { Open = o.Status != Closed });
}
