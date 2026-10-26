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
        var policy = Policy.Handle<InvalidOperationException>().WaitAndRetryForever(attempt =>
        {
            trace.Add("delay " + attempt);
            return TimeSpan.Zero;
        });
        policy.Execute(() =>
        {
            if (++calls < 4)
            {
                throw new InvalidOperationException();
            }
        });
        trace.Add("calls " + calls);
        return Task.FromResult(string.Join(", ", trace));
    }
}
