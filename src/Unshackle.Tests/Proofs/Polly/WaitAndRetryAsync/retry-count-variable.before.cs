using System;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task RunAsync(int retries)
    {
        var policy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(retries, attempt => TimeSpan.FromSeconds(attempt));
        await policy.ExecuteAsync(() => Task.CompletedTask);
    }
}
