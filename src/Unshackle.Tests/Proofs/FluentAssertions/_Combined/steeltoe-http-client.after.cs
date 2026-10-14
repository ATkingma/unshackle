using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Shouldly;

namespace Proofs;

// Shape from Steeltoe (Apache-2.0): HttpClientExtensionsTest and ConfigServerConfigurationProviderTest.
public class HttpClientCase
{
    public void Test(HttpClient httpClient, string userAgent, Func<int> eventCount)
    {
        httpClient.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        httpClient.DefaultRequestHeaders.UserAgent.ShouldHaveSingleItem().ToString().ShouldBe(userAgent);
        SpinWait.SpinUntil(() => eventCount() == 1, TimeSpan.FromSeconds(5)).ShouldBeTrue();
    }

    public void Credentials(Dictionary<string, string> credentials)
    {
        credentials.ShouldContainKey("uri");
        credentials["uri"].ShouldBe("http://localhost:8888");
    }
}
