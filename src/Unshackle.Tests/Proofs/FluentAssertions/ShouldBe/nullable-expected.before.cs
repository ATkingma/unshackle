using FluentAssertions;

namespace Proofs;

public class NullableExpected
{
    public void Test(int count, int? expected)
    {
        count.Should().Be(expected);
    }
}
