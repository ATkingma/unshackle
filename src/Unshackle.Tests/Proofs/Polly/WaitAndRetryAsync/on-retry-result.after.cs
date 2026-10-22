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
        var outcomes = new Queue<int>([-1, 0, 7]);
        var policy = new ResiliencePipelineBuilder<int>()
            .AddRetry(new RetryStrategyOptions<int>
            {
                ShouldHandle = new PredicateBuilder<int>().HandleResult(r => r < 0).Handle<TimeoutException>(),
                MaxRetryAttempts = 3,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => TimeSpan.Zero;
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
                OnRetry = args =>
                {
                    void OnRetryCallback(Outcome<int> outcome, TimeSpan unused1) => trace.Add("retry " + outcome.Result + " " + (outcome.Exception?.GetType().Name ?? "none"));
                    OnRetryCallback(args.Outcome, args.RetryDelay);
                    return default;
                },
            })
            .Build();
        int result = await policy.ExecuteAsync(async _ =>
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
