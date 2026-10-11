using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(string text)
    {
        text.Trim().ToUpperInvariant().Should().Be("A");
    }
}
