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
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => TimeSpan.Zero;
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
                OnRetry = args =>
                {
                    async Task OnRetryCallback(Exception exception, TimeSpan unused1)
                    {
                        await Task.Yield();
                        trace.Add("retry after " + exception.Message);
                    }
                    return new ValueTask(OnRetryCallback(args.Outcome.Exception!, args.RetryDelay));
                },
            })
            .Build();
        int result = await policy.ExecuteAsync(async _ =>
        {
            calls++;
            await Task.Yield();
            if (calls < 3)
            {
                throw new TimeoutException("call " + calls);
            }

            return calls;
        });
        trace.Add("result " + result);
        return string.Join(", ", trace);
    }
}
