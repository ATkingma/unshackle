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
        int value = ResiliencePipeline.Empty.Execute(() => 6);
        trace.Add("value " + value);
        return Task.FromResult(string.Join(", ", trace));
    }
}
