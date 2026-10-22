using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

public static class Proof
{
    public static Task<string> RunAsync()
    {
        var trace = new List<string>();
        int calls = 0;
        var policy = Policy
            .Handle<InvalidOperationException>()
            .WaitAndRetry(3, attempt =>
            {
                trace.Add("delay " + attempt);
                return TimeSpan.FromMilliseconds(attempt);
            });
        try
        {
            policy.Execute(() =>
            {
                calls++;
                throw new InvalidOperationException("call " + calls);
            });
        }
        catch (InvalidOperationException e)
        {
            trace.Add("caught " + e.Message);
        }

        int value = policy.Execute(() => ++calls);
        trace.Add("value " + value);
        return Task.FromResult(string.Join(", ", trace));
    }
}
