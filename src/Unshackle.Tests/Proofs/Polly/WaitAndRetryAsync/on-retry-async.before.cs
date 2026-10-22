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
        var policy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, attempt => TimeSpan.Zero, async (exception, _) =>
        {
            await Task.Yield();
            trace.Add("retry after " + exception.Message);
        });
        int result = await policy.ExecuteAsync(async () =>
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
