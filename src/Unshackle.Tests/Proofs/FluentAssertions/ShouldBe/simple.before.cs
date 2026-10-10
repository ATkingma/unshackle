using FluentAssertions;

namespace Proofs;

public class Simple
{
    public void Test(int result, string name)
    {
        result.Should().Be(42);
        name.Should().Be("timme");
    }
}
