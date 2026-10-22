using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

public static class Proof
{
    public static async Task<string> RunAsync()
    {
        var trace = new List<string>();
        var services = new ServiceCollection();
        services.AddHttpClient("api")
            .ConfigurePrimaryHttpMessageHandler(() => new FakeHandler(trace, [502, 504, 200]))
            .AddResilienceHandler("retry", builder => builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>().Handle<HttpRequestException>().HandleResult(response => (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout),
                    MaxRetryAttempts = 1,
                    DelayGenerator = args =>
                    {
                        TimeSpan SleepDuration(int attempt) => TimeSpan.FromMilliseconds(attempt);
                        return new ValueTask<TimeSpan?>(SleepDuration(args.AttemptNumber + 1));
                    },
                }));
        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("api");
        HttpResponseMessage result = await client.GetAsync("http://localhost/");
        trace.Add("final " + (int)result.StatusCode);
        return string.Join(", ", trace);
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
