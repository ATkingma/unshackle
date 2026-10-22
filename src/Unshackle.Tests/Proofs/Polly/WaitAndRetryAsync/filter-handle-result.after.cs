using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var outcomes = new Queue<int>([-1, 0, 5, -2, -3, -4]);
        var policy = new ResiliencePipelineBuilder<int>()
            .AddRetry(new RetryStrategyOptions<int>
            {
                ShouldHandle = new PredicateBuilder<int>().HandleResult(r => r < 0).Handle<TimeoutException>(),
                MaxRetryAttempts = 2,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => TimeSpan.Zero;
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
            })
            .Build();
        for (int run = 0; run < 2; run++)
        {
            int result = await policy.ExecuteAsync(async _ =>
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
