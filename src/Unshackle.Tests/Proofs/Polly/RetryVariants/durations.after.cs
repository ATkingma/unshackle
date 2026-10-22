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
                MaxRetryAttempts = 3,
                DelayGenerator = args => new ValueTask<TimeSpan?>(args.AttemptNumber switch { 0 => TimeSpan.Zero, 1 => TimeSpan.FromMilliseconds(1), _ => TimeSpan.FromMilliseconds(2) }),
                OnRetry = args =>
                {
                    void OnRetryCallback(Exception unused0, TimeSpan delay) => trace.Add("wait " + delay.TotalMilliseconds);
                    OnRetryCallback(args.Outcome.Exception!, args.RetryDelay);
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
