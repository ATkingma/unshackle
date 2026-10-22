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
                ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero,
                OnRetry = args =>
                {
                    void OnRetryCallback(Exception exception, int retryCount) => trace.Add("retry " + retryCount + " " + exception.Message);
                    OnRetryCallback(args.Outcome.Exception!, args.AttemptNumber + 1);
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
                throw new TimeoutException("call " + calls);
            });
        }
        catch (TimeoutException e)
        {
            trace.Add("caught " + e.Message);
        }

        return string.Join(", ", trace);
    }
}
