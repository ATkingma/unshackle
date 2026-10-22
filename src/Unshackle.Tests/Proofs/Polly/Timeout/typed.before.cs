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
        var policy = Policy.TimeoutAsync<int>(TimeSpan.FromMilliseconds(200));
        try
        {
            await policy.ExecuteAsync(async ct =>
            {
                await Task.Delay(3000, ct);
                return 1;
            }, CancellationToken.None);
        }
        catch (TimeoutRejectedException)
        {
            trace.Add("timed out");
        }
        return string.Join(", ", trace);
    }
}
