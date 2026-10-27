using System;
using Microsoft.Extensions.DependencyInjection;
using Polly;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddRetryPolicy(this IHttpClientBuilder builder)
        => builder.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
}
