using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    private static readonly IAsyncPolicy Retry = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(attempt));

    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        int calls = 0;
        await Retry.ExecuteAsync(async () =>
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
            await Retry.ExecuteAsync(() => FailAsync(trace));
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
