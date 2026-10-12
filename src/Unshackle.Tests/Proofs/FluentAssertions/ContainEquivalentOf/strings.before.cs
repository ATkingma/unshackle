using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(string[] x)
    {
        x.Should().ContainEquivalentOf("abc");
    }
}
