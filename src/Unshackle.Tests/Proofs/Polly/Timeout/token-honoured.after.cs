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
        var policy = new ResiliencePipelineBuilder().AddTimeout(TimeSpan.FromMilliseconds(200)).Build();
        try
        {
            await policy.ExecuteAsync(async ct => await Task.Delay(3000, ct), CancellationToken.None);
        }
        catch (TimeoutRejectedException)
        {
            trace.Add("timed out");
        }

        int fast = await policy.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
            return 7;
        }, CancellationToken.None);
        trace.Add("fast " + fast);
        return string.Join(", ", trace);
    }
}
