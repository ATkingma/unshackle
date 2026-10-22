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
        var policy = Policy<int>.Handle<Exception>().FallbackAsync(0);
        await policy.ExecuteAsync(() => Task.FromResult(1));
        return string.Join(", ", trace);
    }
}
