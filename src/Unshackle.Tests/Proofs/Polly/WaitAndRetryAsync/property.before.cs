using System;
using System.Threading.Tasks;
using Polly;

public class Worker
{
    public IAsyncPolicy Retry { get; } = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, attempt => TimeSpan.Zero);

    public async Task RunAsync() => await Retry.ExecuteAsync(() => Task.CompletedTask);
}
