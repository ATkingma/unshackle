using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var context = new Context("operation");
        trace.Add(context.OperationKey);
        return string.Join(", ", trace);
    }
}
