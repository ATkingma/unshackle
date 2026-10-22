using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

public static class Registration
{
    public static void Add(IServiceCollection services) =>
        services.AddHttpClient("api")
            .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(attempt)))
            .AddTransientHttpErrorPolicy(p => p.RetryAsync(1));
}
