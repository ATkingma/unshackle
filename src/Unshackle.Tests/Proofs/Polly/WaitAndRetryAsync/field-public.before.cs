using System;
using System.Threading.Tasks;
using Polly;

public class Policies
{
    public static readonly IAsyncPolicy Retry = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, attempt => TimeSpan.Zero);

    public async Task RunAsync() => await Retry.ExecuteAsync(() => Task.CompletedTask);
}
