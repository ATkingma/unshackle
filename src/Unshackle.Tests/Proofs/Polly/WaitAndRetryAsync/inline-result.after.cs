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
        int result = await new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
                MaxRetryAttempts = 5,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => TimeSpan.FromMilliseconds(Math.Pow(2, attempt));
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
            })
            .Build().ExecuteAsync(async _ =>
        {
            calls++;
            trace.Add("call " + calls);
            await Task.Yield();
            if (calls < 3)
            {
                throw new TimeoutException();
            }

            return calls * 10;
        });
        trace.Add("result " + result);
        return string.Join(", ", trace);
    }
}
