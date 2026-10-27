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
}
