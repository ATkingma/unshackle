using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var outcomes = new Queue<int>([-1, 0, 5, -2, -3, -4]);
        var policy = Policy.HandleResult<int>(r => r < 0).Or<TimeoutException>().WaitAndRetryAsync(2, attempt => TimeSpan.Zero);
        for (int run = 0; run < 2; run++)
        {
            int result = await policy.ExecuteAsync(async () =>
            {
                await Task.Yield();
                int next = outcomes.Dequeue();
                trace.Add("got " + next);
                if (next == 0)
                {
                    throw new TimeoutException();
                }

                return next;
            });
            trace.Add("result " + result);
        }

        return string.Join(", ", trace);
    }
}
