using System.Threading.Tasks;
using Xunit;

public class DatabaseFixture : IAsyncLifetime
{
    public int Opened { get; private set; }

    public async ValueTask InitializeAsync()
    {
        await Task.Yield();
        Opened++;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Delay(1);
        Opened--;
    }
}
