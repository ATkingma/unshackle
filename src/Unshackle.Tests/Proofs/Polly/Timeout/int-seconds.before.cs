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
        try
        {
            await Policy.TimeoutAsync(1).ExecuteAsync(async ct => await Task.Delay(5000, ct), CancellationToken.None);
        }
        catch (TimeoutRejectedException)
        {
            trace.Add("timed out");
        }
        return string.Join(", ", trace);
    }
}
