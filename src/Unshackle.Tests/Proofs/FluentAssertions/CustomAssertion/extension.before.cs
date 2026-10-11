using FluentAssertions;
using FluentAssertions.Primitives;

namespace Proofs;

public static class ResultAssertions
{
    public static AndConstraint<StringAssertions> BeFailure(this StringAssertions assertions) => assertions.StartWith("Error");
}

public class Extension
{
    public void Test(string result)
    {
        result.Should().BeFailure();
    }
}
