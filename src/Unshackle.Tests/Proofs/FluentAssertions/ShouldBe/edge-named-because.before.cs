using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(int total)
    {
        total.Should().Be(3, because: "three items were added");
    }
}
