using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class Case
{
    public void Test(int x)
    {
        using var scope = new AssertionScope();
        x.Should().Be(1);
    }
}
