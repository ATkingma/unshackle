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
        var policy = Policy.Timeout(TimeSpan.FromMilliseconds(100));
        int value = policy.Execute(() =>
        {
            Thread.Sleep(200);
            return 5;
        });
        trace.Add("value " + value);
        return Task.FromResult(string.Join(", ", trace));
    }
}
