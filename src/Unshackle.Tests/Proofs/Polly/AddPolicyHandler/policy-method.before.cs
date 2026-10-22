using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var services = new ServiceCollection();
        services.AddHttpClient("api")
            .ConfigurePrimaryHttpMessageHandler(() => new FakeHandler(trace, [502, 504, 200]))
            .AddPolicyHandler(GetRetryPolicy());
        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("api");
        HttpResponseMessage result = await client.GetAsync("http://localhost/");
        trace.Add("final " + (int)result.StatusCode);
        return string.Join(", ", trace);
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(1, attempt => TimeSpan.FromMilliseconds(attempt));
    }
}

public class FakeHandler : HttpMessageHandler
{
    private readonly List<string> _trace;
    private readonly Queue<int> _statuses;

    public FakeHandler(List<string> trace, IEnumerable<int> statuses)
    {
        _trace = trace;
        _statuses = new Queue<int>(statuses);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        int status = _statuses.Dequeue();
        _trace.Add("send " + status);
        return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status));
    }
}
