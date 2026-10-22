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
        try
        {
            await Policy.Handle<TimeoutException>().RetryAsync().ExecuteAsync(() => FailAsync(trace, ++calls));
        }
        catch (TimeoutException e)
        {
            trace.Add("caught " + e.Message);
        }

        return string.Join(", ", trace);
    }

    private static async Task FailAsync(List<string> trace, int call)
    {
        trace.Add("call " + call);
        await Task.Yield();
        throw new TimeoutException("call " + call);
    }
}
