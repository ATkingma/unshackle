using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(object? value) => value.Should().BeNull();
}
