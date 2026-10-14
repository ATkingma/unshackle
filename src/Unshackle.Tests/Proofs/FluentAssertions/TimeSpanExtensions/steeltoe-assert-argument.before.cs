using System;
using System.Net.Http;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Proofs;

// Shape from Steeltoe (Apache-2.0): HttpClientExtensionsTest, httpClient.Timeout.Should().Be(30.Seconds()).
public class AssertArgumentCase
{
    public void Test(HttpClient httpClient)
    {
        httpClient.Timeout.Should().Be(30.Seconds());
    }
}
