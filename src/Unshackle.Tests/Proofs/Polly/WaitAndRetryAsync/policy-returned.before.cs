using System;
using Polly;

public static class Policies
{
    public static IAsyncPolicy Retry() => Policy.Handle<TimeoutException>().WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt));
}
