using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(int? value)
    {
        value.HasValue.Should().BeTrue();
    }
}
