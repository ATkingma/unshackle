using System;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static Task RunAsync()
    {
        var policy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt));
        return policy.ExecuteAsync(() => Task.CompletedTask);
    }
}
