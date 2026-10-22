using System;
using System.Threading.Tasks;
using Polly;

public class Worker
{
    private readonly IAsyncPolicy _retry = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, attempt => TimeSpan.Zero);

    public async Task RunAsync()
    {
        await _retry.ExecuteAsync(() => Task.CompletedTask);
        await RunWithAsync(_retry);
    }

    private static Task RunWithAsync(IAsyncPolicy policy) => policy.ExecuteAsync(() => Task.CompletedTask);
}
