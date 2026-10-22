using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;

public static class Proof
{
    public static IAsyncPolicy Make() => Policy.NoOpAsync();

    public static Task<string> RunAsync() => Task.FromResult("done");
}
