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
        var result = Policy.NoOp().ExecuteAndCapture(() => 1);
        trace.Add(result.Outcome.ToString());
        return Task.FromResult(string.Join(", ", trace));
    }
}
