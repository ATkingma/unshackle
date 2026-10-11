using FluentAssertions;

namespace Proofs;

public class ValueUsedCase
{
    public void Test(string s)
    {
        var c = s.Should().BeEquivalentTo("ab");
    }
}
