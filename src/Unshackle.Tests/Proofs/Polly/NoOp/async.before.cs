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
        var policy = Policy.NoOpAsync();
        int value = await policy.ExecuteAsync(() => Task.FromResult(3));
        trace.Add("value " + value);
        await policy.ExecuteAsync(async () => trace.Add("ran"));
        return string.Join(", ", trace);
    }
}
