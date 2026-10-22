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
        ResiliencePipeline policy = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
                MaxRetryAttempts = 2,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int _) => TimeSpan.Zero;
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
            })
            .Build();
        try
        {
            await policy.ExecuteAsync(_ => new ValueTask(FailAsync(++calls))).ConfigureAwait(false);
        }
        catch (ArgumentException e)
        {
            trace.Add("caught " + e.Message);
        }

        trace.Add("calls " + calls);
        return string.Join(", ", trace);
    }

    private static Task FailAsync(int call) => throw new ArgumentException("call " + call);
}
