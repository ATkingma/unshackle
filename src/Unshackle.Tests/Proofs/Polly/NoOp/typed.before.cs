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
        var policy = Policy.NoOpAsync<int>();
        int value = await policy.ExecuteAsync(() => Task.FromResult(9));
        trace.Add("value " + value);
        return string.Join(", ", trace);
    }
}
