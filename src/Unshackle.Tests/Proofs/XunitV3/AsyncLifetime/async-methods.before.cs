using System.Threading.Tasks;
using Xunit;

public class DatabaseFixture : IAsyncLifetime
{
    public int Opened { get; private set; }

    public async Task InitializeAsync()
    {
        await Task.Yield();
        Opened++;
    }

    public async Task DisposeAsync()
    {
        await Task.Delay(1);
        Opened--;
    }
}
