using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

public static class Proof
{
    public static Task<string> RunAsync() => new Worker(new List<string>()).RunAsync();
}

public class Worker
{
    private readonly List<string> _trace;
    private readonly ResiliencePipeline _policy;
    private int _calls;

    public Worker(List<string> trace)
    {
        _trace = trace;
        this._policy = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
                MaxRetryAttempts = 3,
                DelayGenerator = args =>
                {
                    TimeSpan SleepDuration(int attempt) => TimeSpan.FromMilliseconds(attempt);
                    return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                },
                OnRetry = args =>
                {
                    void OnRetryCallback(Exception unused0, TimeSpan delay) => _trace.Add("retry after " + delay.TotalMilliseconds);
                    OnRetryCallback(args.Outcome.Exception!, args.RetryDelay);
                    return default;
                },
            })
            .Build();
    }

    public async Task<string> RunAsync()
    {
        await _policy.ExecuteAsync(async _ =>
        {
            _calls++;
            await Task.Yield();
            if (_calls < 3)
            {
                throw new TimeoutException();
            }
        });
        _trace.Add("calls " + _calls);
        return string.Join(", ", _trace);
    }
}
