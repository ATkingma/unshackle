using System;
using System.Threading.Tasks;
using Xunit;

public class FileFixture : IAsyncLifetime, IDisposable
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
    }
}
