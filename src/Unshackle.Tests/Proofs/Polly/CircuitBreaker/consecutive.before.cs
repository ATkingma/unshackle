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
        var breaker = Policy.Handle<InvalidOperationException>().CircuitBreakerAsync(3, TimeSpan.FromSeconds(1));
        await breaker.ExecuteAsync(() => Task.CompletedTask);
        return string.Join(", ", trace);
    }
}
