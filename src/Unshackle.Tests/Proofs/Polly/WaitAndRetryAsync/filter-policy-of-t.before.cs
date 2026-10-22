using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var outcomes = new Queue<string>([null, "", "ok"]);
        IAsyncPolicy<string> policy = Policy<string>.Handle<TimeoutException>().OrResult(s => string.IsNullOrEmpty(s)).WaitAndRetryAsync(3, attempt => TimeSpan.Zero);
        string result = await policy.ExecuteAsync(() => Task.FromResult(outcomes.Dequeue()));
        trace.Add("result " + result + " left " + outcomes.Count);
        return string.Join(", ", trace);
    }
}
