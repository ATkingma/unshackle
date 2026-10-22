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
        var errors = new Queue<Exception>([new ArgumentException(), new InvalidOperationException(), new TimeoutException(), new ArgumentException(), new InvalidOperationException()]);
        try
        {
            await new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.Zero,
                    OnRetry = args =>
                    {
                        void OnRetryCallback(Exception unused0, int retryCount) => trace.Add("timeout " + retryCount);
                        OnRetryCallback(args.Outcome.Exception!, args.AttemptNumber + 1);
                        return default;
                    },
                })
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.Zero,
                    OnRetry = args =>
                    {
                        void OnRetryCallback(Exception unused0, int retryCount) => trace.Add("invalid " + retryCount);
                        OnRetryCallback(args.Outcome.Exception!, args.AttemptNumber + 1);
                        return default;
                    },
                })
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<ArgumentException>(),
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.Zero,
                    OnRetry = args =>
                    {
                        void OnRetryCallback(Exception unused0, int retryCount) => trace.Add("argument " + retryCount);
                        OnRetryCallback(args.Outcome.Exception!, args.AttemptNumber + 1);
                        return default;
                    },
                })
                .Build()
                .ExecuteAsync(async _ =>
                {
                    await Task.Yield();
                    if (errors.Count > 0)
                    {
                        throw errors.Dequeue();
                    }
                });
            trace.Add("done");
        }
        catch (Exception e)
        {
            trace.Add("caught " + e.GetType().Name);
        }

        return string.Join(", ", trace);
    }
}
