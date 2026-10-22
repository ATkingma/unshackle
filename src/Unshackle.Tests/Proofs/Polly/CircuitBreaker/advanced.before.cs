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
        var breaker = Policy.Handle<InvalidOperationException>().AdvancedCircuitBreaker(0.5, TimeSpan.FromSeconds(10), 4, TimeSpan.FromSeconds(1));
        breaker.Execute(() => { });
        return Task.FromResult(string.Join(", ", trace));
    }
}
