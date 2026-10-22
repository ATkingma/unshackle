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
        var policy = Policy
            .Handle<InvalidOperationException>()
            .WaitAndRetryAsync(
                2,
                attempt => Delay(trace, attempt),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    trace.Add("retry " + retryCount + " after " + timespan.TotalMilliseconds + " " + exception.Message);
                });
        try
        {
            await policy.ExecuteAsync(async () =>
            {
                calls++;
                await Task.Yield();
                throw new InvalidOperationException("call " + calls);
            });
        }
        catch (InvalidOperationException e)
        {
            trace.Add("caught " + e.Message);
        }

        return string.Join(", ", trace);
    }

    private static TimeSpan Delay(List<string> trace, int attempt)
    {
        trace.Add("delay " + attempt);
        return TimeSpan.FromMilliseconds(attempt);
    }
}
