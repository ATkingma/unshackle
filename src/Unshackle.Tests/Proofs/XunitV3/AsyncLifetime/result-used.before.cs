using System.Threading.Tasks;
using Xunit;

public class QueueFixture : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;
}

public static class Runner
{
    public static void Run(QueueFixture fixture)
    {
        fixture.InitializeAsync().Wait();
        fixture.DisposeAsync().ContinueWith(t => t.Exception);
    }
}
