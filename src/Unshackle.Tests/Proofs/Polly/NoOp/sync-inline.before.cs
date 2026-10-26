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
        int value = Policy.NoOp().Execute(() => 6);
        trace.Add("value " + value);
        return Task.FromResult(string.Join(", ", trace));
    }
}
