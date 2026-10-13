using System.Net.Http;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(HttpResponseMessage response)
    {
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeTrue();
        response.Headers.Contains("Vary").ShouldBeFalse();
    }
}
