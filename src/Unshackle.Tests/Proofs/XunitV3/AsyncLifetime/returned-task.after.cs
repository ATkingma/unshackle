using System.Threading.Tasks;
using Xunit;

public class CacheTests : IAsyncLifetime
{
    private int _items;

    public ValueTask InitializeAsync()
    {
        if (_items > 0)
        {
            return default;
        }

        return new ValueTask(Task.Run(() => _items = 3));
    }

    public ValueTask DisposeAsync() => default;

    [Fact]
    public async Task Items_AreLoaded()
    {
        await InitializeAsync();
        Assert.Equal(3, _items);
    }
}
