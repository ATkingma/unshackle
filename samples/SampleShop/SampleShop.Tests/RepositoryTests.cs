using SampleShop.Data;
using SampleShop.Domain;

namespace SampleShop.Tests;

public class RepositoryTests
{
    [Fact]
    public void Add_StoresOrder()
    {
        var repository = new OrderRepository();

        var id = repository.Add(new Order { CustomerName = "Timme" });

        // fa.have-count on a dictionary → manual: Count or Keys?
        repository.All.Should().HaveCount(1);
        repository.All.Should().ContainKey(id);
        repository.Find(id).Should().NotBeNull();
    }

    [Fact]
    public void Find_UnknownId_ReturnsNull()
    {
        var repository = new OrderRepository();

        repository.Find(42).Should().BeNull();
    }
}
