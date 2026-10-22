using System;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task RunAsync()
    {
        var policy = Policy.HandleInner<TimeoutException>().WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt));
        await policy.ExecuteAsync(() => Task.CompletedTask);
    }
}
