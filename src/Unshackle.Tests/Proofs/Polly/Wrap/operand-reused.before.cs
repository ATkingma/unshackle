using System;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task RunAsync()
    {
        var outer = Policy.Handle<TimeoutException>().RetryAsync(1);
        var inner = Policy.Handle<InvalidOperationException>().RetryAsync(2);
        await outer.WrapAsync(inner).ExecuteAsync(() => Task.CompletedTask);
        await inner.ExecuteAsync(() => Task.CompletedTask);
    }
}
