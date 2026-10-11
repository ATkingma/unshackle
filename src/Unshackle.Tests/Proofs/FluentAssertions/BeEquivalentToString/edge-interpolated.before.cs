using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(string text, string name)
    {
        text.Should().BeEquivalentTo($"hello {name}");
    }
}
