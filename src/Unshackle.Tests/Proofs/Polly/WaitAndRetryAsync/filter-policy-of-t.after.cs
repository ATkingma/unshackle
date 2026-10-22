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
        var outcomes = new Queue<string>([null, "", "ok"]);
        ResiliencePipeline<string> policy = new ResiliencePipelineBuilder<string>()
            .AddRetry(new RetryStrategyOptions<string>
            {
                ShouldHandle = new PredicateBuilder<string>().Handle<TimeoutException>().HandleResult(s => string.IsNullOrEmpty(s)),
                MaxRetryAttempts = 3,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => TimeSpan.Zero;
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
            })
            .Build();
        string result = await policy.ExecuteAsync(_ => new ValueTask<string>(Task.FromResult(outcomes.Dequeue())));
        trace.Add("result " + result + " left " + outcomes.Count);
        return string.Join(", ", trace);
    }
}
