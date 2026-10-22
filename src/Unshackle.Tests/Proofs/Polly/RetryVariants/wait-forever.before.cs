using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        int calls = 0;
        var policy = Policy.HandleResult<int>(r => r < 5).WaitAndRetryForeverAsync(attempt => Delay(trace, attempt));
        int result = await policy.ExecuteAsync(() => Task.FromResult(++calls));
        var forever = Policy.Handle<TimeoutException>().RetryForeverAsync();
        await forever.ExecuteAsync(async () =>
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
