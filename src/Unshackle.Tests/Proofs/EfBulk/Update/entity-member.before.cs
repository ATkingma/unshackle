#nullable enable
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;

public class Order
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public int Retries { get; set; }
    public decimal? Total { get; set; }
    public Customer? Customer { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class ShopContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
}

public class Cleanup
{
    public int Run(ShopContext db) => db.Orders.Update(o => new Order { Retries = o.Id });
}
