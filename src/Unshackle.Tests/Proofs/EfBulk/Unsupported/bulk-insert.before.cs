#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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

public class Import
{
    public void Run(ShopContext db, List<Order> orders) => db.BulkInsert(orders);
}
