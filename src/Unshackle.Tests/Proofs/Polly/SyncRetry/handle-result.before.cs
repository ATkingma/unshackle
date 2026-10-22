using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static Task<string> RunAsync()
    {
        var trace = new List<string>();
        int calls = 0;
        var policy = Policy.HandleResult<int>(r => r < 0).WaitAndRetry(2, attempt => TimeSpan.Zero);
        int result = policy.Execute(() =>
        {
            calls++;
            trace.Add("got " + (calls - 3));
            return calls - 3;
        });
        trace.Add("result " + result);
        return Task.FromResult(string.Join(", ", trace));
    }
}
