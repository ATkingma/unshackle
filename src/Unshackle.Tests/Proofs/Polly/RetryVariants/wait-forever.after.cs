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
                ShouldHandle = new PredicateBuilder<int>().HandleResult(r => r < 5),
                MaxRetryAttempts = int.MaxValue,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => Delay(trace, attempt);
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
            })
            .Build();
        int result = await policy.ExecuteAsync(_ => new ValueTask<int>(Task.FromResult(++calls)));
        var forever = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
                MaxRetryAttempts = int.MaxValue,
                Delay = TimeSpan.Zero,
            })
            .Build();
        await forever.ExecuteAsync(async _ =>
        {
            await Task.Yield();
            if (++calls < 9)
            {
                throw new TimeoutException();
            }
        });
        trace.Add("result " + result + " calls " + calls);
        return string.Join(", ", trace);
    }

    private static TimeSpan Delay(List<string> trace, int attempt)
    {
        trace.Add("delay " + attempt);
        return TimeSpan.Zero;
    }
}
