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
        var policy = ResiliencePipeline<int>.Empty;
        int value = await policy.ExecuteAsync(_ => new ValueTask<int>(Task.FromResult(9)));
        trace.Add("value " + value);
        return string.Join(", ", trace);
    }
}
