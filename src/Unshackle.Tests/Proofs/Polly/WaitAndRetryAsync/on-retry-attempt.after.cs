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
                MaxRetryAttempts = 2,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => Delay(trace, attempt);
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
                OnRetry = args =>
                {
                    void OnRetryCallback(Exception exception, TimeSpan timespan, int retryCount)
                    {
                        trace.Add("retry " + retryCount + " after " + timespan.TotalMilliseconds + " " + exception.Message);
                    }
                    OnRetryCallback(args.Outcome.Exception!, args.RetryDelay, args.AttemptNumber + 1);
                    return default;
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

        return string.Join(", ", trace);
    }

    private static TimeSpan Delay(List<string> trace, int attempt)
    {
        trace.Add("delay " + attempt);
        return TimeSpan.FromMilliseconds(attempt);
    }
}
