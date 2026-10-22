using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        int calls = 0;
        var policy = Policy
            .Handle<InvalidOperationException>()
            .WaitAndRetryAsync(3, attempt =>
            {
                trace.Add("delay " + attempt);
                return TimeSpan.FromMilliseconds(attempt);
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

        trace.Add("calls " + calls);
        return string.Join(", ", trace);
    }
}
