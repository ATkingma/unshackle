using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(bool ok, string name)
    {
        ok.Should().BeTrue($"{name} must be valid");
    }
}
