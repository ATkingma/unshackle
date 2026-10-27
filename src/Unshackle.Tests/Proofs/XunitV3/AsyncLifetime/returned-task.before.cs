using System.Threading.Tasks;
using Xunit;

public class CacheTests : IAsyncLifetime
{
    private int _items;

    public Task InitializeAsync()
    {
        if (_items > 0)
        {
            return Task.CompletedTask;
        }

        return Task.Run(() => _items = 3);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Items_AreLoaded()
    {
        await InitializeAsync();
        Assert.Equal(3, _items);
    }
}
