using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var errors = new Queue<Exception>([new ArgumentException(), new InvalidOperationException(), new TimeoutException(), new ArgumentException(), new InvalidOperationException()]);
        try
        {
            await Policy.WrapAsync(
                Policy.Handle<TimeoutException>().RetryAsync(1, (exception, retryCount) => trace.Add("timeout " + retryCount)),
                Policy.Handle<InvalidOperationException>().RetryAsync(1, (exception, retryCount) => trace.Add("invalid " + retryCount)),
                Policy.Handle<ArgumentException>().RetryAsync(1, (exception, retryCount) => trace.Add("argument " + retryCount)))
                .ExecuteAsync(async () =>
                {
                    await Task.Yield();
                    if (errors.Count > 0)
                    {
                        throw errors.Dequeue();
                    }
                });
            trace.Add("done");
        }
        catch (Exception e)
        {
            trace.Add("caught " + e.GetType().Name);
        }

        return string.Join(", ", trace);
    }
}
