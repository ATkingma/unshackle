using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(string url, string root)
    {
        url.Should().Be($"{root}/license");
    }
}
