using System;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task RunAsync()
    {
        var policy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(3, attempt => TimeSpan.Zero, (exception, delay, context) => Console.WriteLine(context.OperationKey));
        await policy.ExecuteAsync(() => Task.CompletedTask);
    }
}
