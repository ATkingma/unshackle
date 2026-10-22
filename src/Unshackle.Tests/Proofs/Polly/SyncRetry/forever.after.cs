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
        var policy = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                MaxRetryAttempts = int.MaxValue,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt)
                    {
                        trace.Add("delay " + attempt);
                        return TimeSpan.Zero;
                    }
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
            })
            .Build();
        policy.Execute(() =>
        {
            if (++calls < 4)
            {
                throw new InvalidOperationException();
            }
        });
        trace.Add("calls " + calls);
        return Task.FromResult(string.Join(", ", trace));
    }
}
