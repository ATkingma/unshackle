using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class Scope
{
    public void Test(int x)
    {
        using var scope = new AssertionScope();
        x.Should().NotBe(1);
    }
}
