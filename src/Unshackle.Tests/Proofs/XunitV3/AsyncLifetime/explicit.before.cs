using System.Threading.Tasks;
using Xunit;

public class ServerFixture : IAsyncLifetime
{
    public bool Started { get; private set; }

    async System.Threading.Tasks.Task IAsyncLifetime.InitializeAsync()
    {
        await Task.Yield();
        Started = true;
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        Started = false;
        return Task.CompletedTask;
    }
}
