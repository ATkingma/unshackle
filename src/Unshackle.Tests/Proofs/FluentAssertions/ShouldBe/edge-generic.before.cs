using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test<T>(T value, T expected)
    {
        value.Should().Be(expected);
    }
}
