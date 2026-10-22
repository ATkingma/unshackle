using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;

public static class Proof
{
    public static Task<string> RunAsync()
    {
        var trace = new List<string>();
        var policy = new ResiliencePipelineBuilder().AddTimeout(TimeSpan.FromMilliseconds(100)).Build();
        int value = policy.Execute(() =>
        {
            Thread.Sleep(200);
            return 5;
        });
        trace.Add("value " + value);
        return Task.FromResult(string.Join(", ", trace));
    }
}
