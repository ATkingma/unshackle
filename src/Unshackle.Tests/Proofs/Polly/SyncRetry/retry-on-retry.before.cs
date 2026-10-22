using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static Task<string> RunAsync()
    {
        var trace = new List<string>();
        int calls = 0;
        try
        {
            Policy.Handle<TimeoutException>().Retry(2, (exception, retryCount) => trace.Add("retry " + retryCount + " " + exception.Message)).Execute(() =>
            {
                calls++;
                throw new TimeoutException("call " + calls);
            });
        }
        catch (TimeoutException e)
        {
            trace.Add("caught " + e.Message);
        }

        return Task.FromResult(string.Join(", ", trace));
    }
}
