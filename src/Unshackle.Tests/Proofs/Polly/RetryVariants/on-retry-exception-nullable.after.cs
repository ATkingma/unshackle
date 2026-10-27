#nullable enable
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
        var policy = new ResiliencePipelineBuilder<int>()
            .AddRetry(new RetryStrategyOptions<int>
            {
                ShouldHandle = new PredicateBuilder<int>().Handle<TimeoutException>(),
                MaxRetryAttempts = 2,
                Delay = TimeSpan.Zero,
                OnRetry = args =>
                {
                    void OnRetryCallback(Outcome<int> result, int retryCount)
                    {
                        string message = ((TimeoutException)result.Exception!).Message;
                        trace.Add("retry " + retryCount + " " + message + " " + (result.Exception?.GetType().Name ?? "none"));
                    }
                    OnRetryCallback(args.Outcome, args.AttemptNumber + 1);
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
                if (calls > 0)
                {
                    throw new TimeoutException("call " + calls);
                }

                return calls;
            });
        }
        catch (TimeoutException e)
        {
            trace.Add("caught " + e.Message);
        }

        return string.Join(", ", trace);
    }
}
