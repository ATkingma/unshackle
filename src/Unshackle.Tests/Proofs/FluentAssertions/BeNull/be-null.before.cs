using FluentAssertions;

namespace Proofs;

public class BeNullProof
{
    public void Test(string? text)
    {
        text.Should().BeNull();
    }
}
