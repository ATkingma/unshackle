using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

// Shape from Steeltoe (Apache-2.0): ConfigServerConfigurationProviderTest, a non-cancellation exception type (TAAK-221).
public class HttpRequestExceptionCase
{
    public async Task Test(Func<Task> action)
    {
        await action.Should().ThrowExactlyAsync<HttpRequestException>();
    }
}
