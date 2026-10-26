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
        var policy = new ResiliencePipelineBuilder().AddTimeout(TimeSpan.FromMilliseconds(100)).Build();
        await policy.ExecuteAsync(async _ => await Task.Delay(300));
        trace.Add("completed");
        int value = await policy.ExecuteAsync(_ => new ValueTask<int>(Task.FromResult(4)));
        trace.Add("value " + value);
        return string.Join(", ", trace);
    }
}
