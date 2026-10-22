using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var outcomes = new Queue<int>([-1, 0, 7]);
        var policy = Policy.HandleResult<int>(r => r < 0).Or<TimeoutException>().WaitAndRetryAsync(3, attempt => TimeSpan.Zero, (outcome, delay) => trace.Add("retry " + outcome.Result + " " + (outcome.Exception?.GetType().Name ?? "none")));
        int result = await policy.ExecuteAsync(async () =>
        {
            await Task.Yield();
            int next = outcomes.Dequeue();
            if (next == 0)
            {
                throw new TimeoutException();
            }

            return next;
        });
        trace.Add("result " + result);
        return string.Join(", ", trace);
    }
}
