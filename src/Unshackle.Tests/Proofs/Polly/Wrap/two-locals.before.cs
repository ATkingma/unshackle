using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var errors = new Queue<Exception>([new InvalidOperationException(), new InvalidOperationException(), new TimeoutException(), new InvalidOperationException()]);
        var outer = Policy.Handle<TimeoutException>().RetryAsync(1, (exception, retryCount) => trace.Add("outer " + retryCount));
        var inner = Policy.Handle<InvalidOperationException>().RetryAsync(2, (exception, retryCount) => trace.Add("inner " + retryCount));
        var policy = outer.WrapAsync(inner);
        try
        {
            await policy.ExecuteAsync(async () =>
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
