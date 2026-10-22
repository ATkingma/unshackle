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
        var policy = Policy.TimeoutAsync(TimeSpan.FromSeconds(1));
        await policy.ExecuteAsync(ct => Task.Delay(10, ct), CancellationToken.None);
        return string.Join(", ", trace);
    }
}
