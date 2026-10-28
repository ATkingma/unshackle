using System.Threading.Tasks;
using Xunit;

public class QueueFixture : IAsyncLifetime
{
    public ValueTask InitializeAsync() => default;

    public ValueTask DisposeAsync() => default;
}

public static class Runner
{
    public static Task RunAsync(QueueFixture fixture) => Task.WhenAll(fixture.InitializeAsync().AsTask(), fixture.DisposeAsync().AsTask());

    public static Task StartAsync(IAsyncLifetime lifetime)
    {
        Task started = lifetime.InitializeAsync().AsTask();
        return started;
    }

    public static Task StopAsync(QueueFixture fixture)
    {
        return fixture.DisposeAsync().AsTask();
    }
}
