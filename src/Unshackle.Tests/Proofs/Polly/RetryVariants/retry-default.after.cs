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
        try
        {
            await new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.Zero,
                })
                .Build().ExecuteAsync(_ => new ValueTask(FailAsync(trace, ++calls)));
        }
        catch (TimeoutException e)
        {
            trace.Add("caught " + e.Message);
        }

        return string.Join(", ", trace);
    }

    private static async Task FailAsync(List<string> trace, int call)
    {
        trace.Add("call " + call);
        await Task.Yield();
        throw new TimeoutException("call " + call);
    }
}
