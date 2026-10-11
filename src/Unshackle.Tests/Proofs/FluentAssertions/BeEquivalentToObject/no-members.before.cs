using FluentAssertions;

namespace Proofs;

public class Hidden
{
    internal int X { get; set; }
}

public class Case
{
    public void Test(Hidden a, Hidden b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
