using FluentAssertions;

namespace Proofs;

public class ArrayCase
{
    public void Test(int[] x)
    {
        x.Should().HaveCount(3);
    }
}
