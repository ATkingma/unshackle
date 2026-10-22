using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var errors = new Queue<Exception>([new TimeoutException("t"), new InvalidOperationException("go"), new InvalidOperationException("stop")]);
        var policy = Policy.Handle<InvalidOperationException>(e => e.Message != "stop").Or<TimeoutException>().WaitAndRetryAsync(5, attempt => TimeSpan.Zero);
        try
        {
            await policy.ExecuteAsync(async () =>
            {
                await Task.Yield();
                Exception error = errors.Dequeue();
                trace.Add("throw " + error.Message);
                throw error;
            });
        }
        catch (Exception e)
        {
            trace.Add("caught " + e.GetType().Name + " " + e.Message);
        }

        return string.Join(", ", trace);
    }
}
