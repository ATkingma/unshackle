#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        int calls = 0;
        var policy = Policy<int>.Handle<TimeoutException>().RetryAsync(2, (result, retryCount) =>
        {
            string message = ((TimeoutException)result.Exception).Message;
            trace.Add("retry " + retryCount + " " + message + " " + (result.Exception?.GetType().Name ?? "none"));
        });
        try
        {
            await policy.ExecuteAsync(async () =>
            {
                calls++;
                await Task.Yield();
                if (calls > 0)
                {
                    throw new TimeoutException("call " + calls);
                }

                return calls;
            });
        }
        catch (TimeoutException e)
        {
            trace.Add("caught " + e.Message);
        }

        return string.Join(", ", trace);
    }
}
