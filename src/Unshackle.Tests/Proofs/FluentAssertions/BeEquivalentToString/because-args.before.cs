using FluentAssertions;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(string s)
    {
        s.Should().BeEquivalentTo("ab", "reason {0}", 1);
    }
}
