using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

public static class Proof
{
    public static Task<string> RunAsync() => new Worker().RunAsync();
}

public class Worker
{
    private readonly ResiliencePipeline<int> _policy = new ResiliencePipelineBuilder<int>()
        .AddRetry(new RetryStrategyOptions<int>
        {
            ShouldHandle = new PredicateBuilder<int>().HandleResult(r => r < 0),
            MaxRetryAttempts = 2,
            Delay = TimeSpan.Zero,
        })
        .Build();

    private readonly Queue<int> _outcomes = new([-1, -2, 3, -4, -5, -6]);

    public async Task<string> RunAsync()
    {
        int first = await this._policy.ExecuteAsync(_ => new ValueTask<int>(Task.FromResult(_outcomes.Dequeue())));
        int second = await _policy.ExecuteAsync(_ => new ValueTask<int>(Task.FromResult(_outcomes.Dequeue())));
        return "first " + first + ", second " + second + ", left " + _outcomes.Count;
    }
}
