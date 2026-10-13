using System.Net.Http;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(HttpResponseMessage response)
    {
        response.Headers.Should().ContainKey("Access-Control-Allow-Origin");
        response.Headers.Should().NotContainKey("Vary");
    }
}
