using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Proofs;

public static class ScopedResultAssertions
{
    public static AndConstraint<StringAssertions> BeFailure(this StringAssertions assertions) => assertions.StartWith("Error");
}

public class InAssertionScope
{
    public void Test(string result, int count)
    {
        using (new AssertionScope())
        {
            result.Should().BeFailure();
            count.Should().Be(1);
        }
    }
}
