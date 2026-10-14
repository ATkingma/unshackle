using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Proofs;

// Shape from Steeltoe (Apache-2.0): HttpClientExtensionsTest and ConfigServerConfigurationProviderTest.
public class HttpClientCase
{
    public void Test(HttpClient httpClient, string userAgent, Func<int> eventCount)
    {
        httpClient.Timeout.Should().Be(30.Seconds());
        httpClient.DefaultRequestHeaders.UserAgent.Should().ContainSingle().Which.ToString().Should().Be(userAgent);
        SpinWait.SpinUntil(() => eventCount() == 1, 5.Seconds()).Should().BeTrue();
    }

    public void Credentials(Dictionary<string, string> credentials)
    {
        credentials.Should().ContainKey("uri").WhoseValue.Should().Be("http://localhost:8888");
    }
}
