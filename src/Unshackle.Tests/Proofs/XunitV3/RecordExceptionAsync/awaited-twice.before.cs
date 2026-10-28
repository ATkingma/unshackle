using System;
using System.Threading.Tasks;
using Xunit;

public class ServiceTests
{
    [Fact]
    public async Task StoredResult_AwaitedTwice_SameException()
    {
        Task<Exception> pending = Record.ExceptionAsync(Fail);
        Exception first = await pending;
        Exception second = await pending;
        Assert.Same(first, second);
        Assert.IsType<InvalidOperationException>(first);
    }

    private static async Task Fail()
    {
        await Task.Yield();
        throw new InvalidOperationException("boom");
    }
}
