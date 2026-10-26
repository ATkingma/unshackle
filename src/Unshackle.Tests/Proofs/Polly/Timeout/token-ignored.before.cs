using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var policy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(100));
        await policy.ExecuteAsync(async () => await Task.Delay(300));
        trace.Add("completed");
        int value = await policy.ExecuteAsync(() => Task.FromResult(4));
        trace.Add("value " + value);
        return string.Join(", ", trace);
    }
}
