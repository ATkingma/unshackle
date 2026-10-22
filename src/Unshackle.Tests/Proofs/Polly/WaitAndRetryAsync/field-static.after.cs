using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

public static class Proof
{
    private static readonly ResiliencePipeline Retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
            MaxRetryAttempts = 2,
            DelayGenerator = args =>
            {
                TimeSpan SleepDuration(int attempt) => TimeSpan.FromMilliseconds(attempt);
                return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
            },
        })
        .Build();

    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        int calls = 0;
        await Retry.ExecuteAsync(async _ =>
        {
            calls++;
            trace.Add("call " + calls);
            await Task.Yield();
            if (calls < 2)
            {
                throw new TimeoutException();
            }
        });
        try
        {
            await Retry.ExecuteAsync(_ => new ValueTask(FailAsync(trace)));
        }
        catch (TimeoutException e)
        {
            trace.Add("caught " + e.Message);
        }

        return string.Join(", ", trace);
    }

    private static Task FailAsync(List<string> trace)
    {
        trace.Add("fail");
        return Task.FromException(new TimeoutException("always"));
    }
}
