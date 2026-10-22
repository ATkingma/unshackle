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
        int result = await Policy.Handle<TimeoutException>().WaitAndRetryAsync(5, attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt))).ExecuteAsync(async () =>
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
