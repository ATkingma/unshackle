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
    private readonly AsyncRetryPolicy _policy;
    private int _calls;

    public Worker(List<string> trace)
    {
        _trace = trace;
        this._policy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(attempt), (exception, delay) => _trace.Add("retry after " + delay.TotalMilliseconds));
    }

    public async Task<string> RunAsync()
    {
        await _policy.ExecuteAsync(async () =>
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
