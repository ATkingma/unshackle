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
        IAsyncPolicy policy = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, _ => TimeSpan.Zero);
        try
        {
            await policy.ExecuteAsync(() => FailAsync(++calls)).ConfigureAwait(false);
        }
        catch (ArgumentException e)
        {
            trace.Add("caught " + e.Message);
        }

        trace.Add("calls " + calls);
        return string.Join(", ", trace);
    }

    private static Task FailAsync(int call) => throw new ArgumentException("call " + call);
}
