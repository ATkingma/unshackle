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
        int calls = 0;
        var policy = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                MaxRetryAttempts = 3,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt)
                    {
                        trace.Add("delay " + attempt);
                        return TimeSpan.FromMilliseconds(attempt);
                    }
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
            })
            .Build();
        try
        {
            await policy.ExecuteAsync(async _ =>
            {
                calls++;
                await Task.Yield();
                throw new InvalidOperationException("call " + calls);
            });
        }
        catch (InvalidOperationException e)
        {
            trace.Add("caught " + e.Message);
        }

        trace.Add("calls " + calls);
        return string.Join(", ", trace);
    }
}
