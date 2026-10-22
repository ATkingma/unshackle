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
        var errors = new Queue<Exception>([new TimeoutException("t"), new InvalidOperationException("go"), new InvalidOperationException("stop")]);
        var policy = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(e => e.Message != "stop").Handle<TimeoutException>(),
                MaxRetryAttempts = 5,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => TimeSpan.Zero;
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
            })
            .Build();
        try
        {
            await policy.ExecuteAsync(async _ =>
            {
                await Task.Yield();
                Exception error = errors.Dequeue();
                trace.Add("throw " + error.Message);
                throw error;
            });
        }
        catch (Exception e)
        {
            trace.Add("caught " + e.GetType().Name + " " + e.Message);
        }

        return string.Join(", ", trace);
    }
}
