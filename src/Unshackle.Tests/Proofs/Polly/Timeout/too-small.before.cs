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
        var policy = Policy.TimeoutAsync(TimeSpan.FromMilliseconds(5));
        await policy.ExecuteAsync(() => Task.CompletedTask);
        return string.Join(", ", trace);
    }
}
