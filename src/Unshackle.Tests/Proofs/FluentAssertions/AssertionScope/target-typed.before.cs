using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class Case
{
    public void Test(int x)
    {
        using AssertionScope scope = new();
        x.Should().Be(1);
    }
}
