using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;

public static class Proof
{
    public static Task<string> RunAsync()
    {
        var policy = Policy.Handle<TimeoutException>().Retry(1);
        policy.Execute(context => { }, new Context("op"));
        return Task.FromResult("done");
    }
}
