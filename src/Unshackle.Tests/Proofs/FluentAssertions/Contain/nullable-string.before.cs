#nullable enable
using FluentAssertions;

namespace Proofs;

public class NullableContain
{
    public void Test(string? text)
    {
        text.Should().Contain("ab");
    }
}
