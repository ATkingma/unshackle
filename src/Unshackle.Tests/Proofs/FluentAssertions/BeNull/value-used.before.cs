using FluentAssertions;

namespace Proofs;

public class ValueUsed
{
    public void Test(string? text)
    {
        var constraint = text.Should().NotBeNull();
    }
}
