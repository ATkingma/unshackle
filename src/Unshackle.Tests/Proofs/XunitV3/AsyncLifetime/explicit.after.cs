using System;
using System.Threading.Tasks;
using Xunit;

public class ServerFixture : IAsyncLifetime
{
    public bool Started { get; private set; }

    async System.Threading.Tasks.ValueTask IAsyncLifetime.InitializeAsync()
    {
        await Task.Yield();
        Started = true;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Started = false;
        return default;
    }
}
