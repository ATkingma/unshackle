using System;
using System.Threading.Tasks;
using Xunit;

public class ServiceTests
{
    [Fact]
    public async Task Awaited_StaysAsItIs()
    {
        Exception error = await Record.ExceptionAsync(() => Task.CompletedTask);
        Exception configured = await Record.ExceptionAsync(() => Task.CompletedTask).ConfigureAwait(false);
        Assert.Null(error);
        Assert.Null(configured);
    }

    [Fact]
    public async Task NotAwaited_KeepsTheTask()
    {
        Task<Exception> pending = Record.ExceptionAsync(() => Task.CompletedTask);
        var later = Record.ExceptionAsync(() => Task.CompletedTask);
        Exception[] errors = await Task.WhenAll(pending, later);
        Assert.All(errors, Assert.Null);
        Assert.Null(Record.ExceptionAsync(() => Task.CompletedTask).Result);
    }

    private static Task<Exception> Check(Func<Task> code) => Record.ExceptionAsync(code);
}
