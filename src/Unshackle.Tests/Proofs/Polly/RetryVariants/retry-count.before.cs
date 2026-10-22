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
        var policy = Policy.Handle<TimeoutException>().RetryAsync(2, (exception, retryCount) => trace.Add("retry " + retryCount + " " + exception.Message));
        try
        {
            await policy.ExecuteAsync(async () =>
            {
                calls++;
                await Task.Yield();
                throw new TimeoutException("call " + calls);
            });
        }
        catch (TimeoutException e)
        {
            trace.Add("caught " + e.Message);
        }

        return string.Join(", ", trace);
    }
}
