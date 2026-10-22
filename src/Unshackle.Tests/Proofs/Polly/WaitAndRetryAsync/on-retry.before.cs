using System;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task RunAsync()
    {
        await Policy.Handle<TimeoutException>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt), (exception, delay) => Console.WriteLine(exception.Message))
            .ExecuteAsync(() => Task.CompletedTask);
    }
}
