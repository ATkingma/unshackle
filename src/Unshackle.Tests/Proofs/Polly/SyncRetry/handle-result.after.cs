using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

public static class Proof
{
    public static Task<string> RunAsync()
    {
        var trace = new List<string>();
        int calls = 0;
        var policy = new ResiliencePipelineBuilder<int>()
            .AddRetry(new RetryStrategyOptions<int>
            {
                ShouldHandle = new PredicateBuilder<int>().HandleResult(r => r < 0),
                MaxRetryAttempts = 2,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => TimeSpan.Zero;
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
            })
            .Build();
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
