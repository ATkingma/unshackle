using System.Threading.Tasks;
using Xunit;

public class QueueFixture : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;
}

public static class Runner
{
    public static Task RunAsync(QueueFixture fixture) => Task.WhenAll(fixture.InitializeAsync(), fixture.DisposeAsync());

    public static Task StartAsync(IAsyncLifetime lifetime)
    {
        Task started = lifetime.InitializeAsync();
        return started;
    }

    public static Task StopAsync(QueueFixture fixture)
    {
        return fixture.DisposeAsync();
    }
}
