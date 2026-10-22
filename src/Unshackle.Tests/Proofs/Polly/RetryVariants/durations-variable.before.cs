using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task RunAsync(IEnumerable<TimeSpan> durations)
    {
        var policy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(durations);
        await policy.ExecuteAsync(() => Task.CompletedTask);
    }
}
