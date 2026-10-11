using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(string text, int id)
    {
        text.Should().Contain($"id={id}");
    }
}
